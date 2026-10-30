namespace CloudGround.App.Api;

internal sealed class AccessTokenRefresher(HttpClient tokenClient, TimeProvider clock)
{
    private static readonly TimeSpan RefreshWindow = TimeSpan.FromMinutes(1);
    private static readonly JsonWebTokenHandler IdTokens = new();

    public async Task<TokenRefresh> RefreshIfCloseToExpiryAsync(AuthenticationProperties properties, OpenIdConnectOptions options, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var expiresAt = properties.GetTokenValue("expires_at") is { } value
            ? DateTimeOffset.Parse(value, CultureInfo.InvariantCulture)
            : now;
        if (expiresAt - now > RefreshWindow)
        {
            return TokenRefresh.NotNeeded;
        }

        if (properties.GetTokenValue("refresh_token") is not { } refreshToken || options.ConfigurationManager is null)
        {
            return TokenRefresh.Failed;
        }

        var stillValid = expiresAt > now ? TokenRefresh.NotNeeded : TokenRefresh.Failed;
        HttpResponseMessage response;
        try
        {
            var configuration = await options.ConfigurationManager.GetConfigurationAsync(cancellationToken);
            using var request = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = options.ClientId!,
                ["client_secret"] = options.ClientSecret!,
                ["refresh_token"] = refreshToken,
            });
            response = await tokenClient.PostAsync(new Uri(configuration.TokenEndpoint), request, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return stillValid;
        }

        using (response)
        {
            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
            {
                return TokenRefresh.Failed;
            }

            if (!response.IsSuccessStatusCode || await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken) is not { } tokens)
            {
                return stillValid;
            }

            if (!BelongToTheSameUser(tokens.IdToken, properties.GetTokenValue("id_token")))
            {
                return TokenRefresh.Failed;
            }

            properties.UpdateTokenValue("id_token", tokens.IdToken);
            properties.UpdateTokenValue("access_token", tokens.AccessToken);
            properties.UpdateTokenValue("refresh_token", tokens.RefreshToken ?? refreshToken);
            properties.UpdateTokenValue("expires_at", now.AddSeconds(tokens.ExpiresIn).ToString("o", CultureInfo.InvariantCulture));
            return TokenRefresh.Refreshed;
        }
    }

    private static bool BelongToTheSameUser(string? idToken, string? sessionIdToken) =>
        Read(idToken) is { IsEncrypted: false } current
        && Read(sessionIdToken) is { IsEncrypted: false } session
        && current.Subject == session.Subject
        && current.Issuer == session.Issuer;

    private static JsonWebToken? Read(string? idToken)
    {
        try
        {
            return IdTokens.CanReadToken(idToken) ? new JsonWebToken(idToken) : null;
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException)
        {
            return null;
        }
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("id_token")] string? IdToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
