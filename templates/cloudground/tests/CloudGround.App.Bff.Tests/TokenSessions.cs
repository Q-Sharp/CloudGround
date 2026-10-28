using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace CloudGround.App.Bff.Tests;

internal static class TokenSessions
{
    public const string NewTokens = """{"access_token":"new-access","refresh_token":"new-refresh","expires_in":300}""";

    public static readonly DateTimeOffset Now = new(2026, 10, 28, 9, 0, 0, TimeSpan.Zero);

    public static AuthenticationProperties ExpiringAt(DateTimeOffset expiresAt)
    {
        var properties = new AuthenticationProperties();
        properties.StoreTokens(
        [
            new AuthenticationToken { Name = "access_token", Value = "old-access" },
            new AuthenticationToken { Name = "refresh_token", Value = "old-refresh" },
            new AuthenticationToken { Name = "expires_at", Value = expiresAt.ToString("o", CultureInfo.InvariantCulture) },
        ]);
        return properties;
    }

    public static OpenIdConnectOptions Options() => new()
    {
        ClientId = "cloudground-app-bff",
        ClientSecret = "secret",
        ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(
            new OpenIdConnectConfiguration { TokenEndpoint = "https://identity.example/token" }),
    };
}
