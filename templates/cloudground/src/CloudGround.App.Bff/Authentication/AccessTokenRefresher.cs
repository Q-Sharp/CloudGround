using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace CloudGround.App.Bff.Authentication;

internal sealed class AccessTokenRefresher(HttpClient tokenClient, TimeProvider clock)
{
    private static readonly TimeSpan RefreshWindow = TimeSpan.FromMinutes(1);

    public async Task<TokenRefresh> RefreshIfCloseToExpiryAsync(AuthenticationProperties properties, OpenIdConnectOptions options, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        if (properties.GetTokenValue("expires_at") is { } expiresAt
            && DateTimeOffset.Parse(expiresAt, CultureInfo.InvariantCulture) - now > RefreshWindow)
        {
            return TokenRefresh.NotNeeded;
        }

        if (properties.GetTokenValue("refresh_token") is not { } refreshToken || options.ConfigurationManager is null)
        {
            return TokenRefresh.Failed;
        }

        var configuration = await options.ConfigurationManager.GetConfigurationAsync(cancellationToken);
        using var request = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = options.ClientId!,
            ["client_secret"] = options.ClientSecret!,
            ["refresh_token"] = refreshToken,
        });
        using var response = await tokenClient.PostAsync(new Uri(configuration.TokenEndpoint), request, cancellationToken);
        if (!response.IsSuccessStatusCode || await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken) is not { } tokens)
        {
            return TokenRefresh.Failed;
        }

        properties.UpdateTokenValue("access_token", tokens.AccessToken);
        properties.UpdateTokenValue("refresh_token", tokens.RefreshToken ?? refreshToken);
        properties.UpdateTokenValue("expires_at", now.AddSeconds(tokens.ExpiresIn).ToString("o", CultureInfo.InvariantCulture));
        return TokenRefresh.Refreshed;
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
