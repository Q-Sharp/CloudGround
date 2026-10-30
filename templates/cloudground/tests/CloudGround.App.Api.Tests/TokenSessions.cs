namespace CloudGround.App.Api.Tests;

internal static class TokenSessions
{
    public static readonly string NewTokens = TokensWithRoles("reader");

    public static readonly DateTimeOffset Now = new(2026, 10, 28, 9, 0, 0, TimeSpan.Zero);

    public const string TokensWithoutIdToken = """{"access_token":"new-access","refresh_token":"new-refresh","expires_in":300}""";

    public static string TokensWithRoles(params string[] roles) => TokensWith(IdToken("alice", roles));

    public static string TokensWith(string idToken) => JsonSerializer.Serialize(new Dictionary<string, object>
    {
        ["access_token"] = "new-access",
        ["refresh_token"] = "new-refresh",
        ["id_token"] = idToken,
        ["expires_in"] = 300,
    });

    public static string IdToken(string subject, string[] roles, string issuer = "https://identity.example") =>
        $"{Encoded(new { alg = "none" })}.{Encoded(new { sub = subject, iss = issuer, roles })}.signature";

    public static AuthenticationProperties ExpiringAt(DateTimeOffset expiresAt)
    {
        var properties = new AuthenticationProperties();
        properties.StoreTokens(
        [
            new AuthenticationToken { Name = "access_token", Value = "old-access" },
            new AuthenticationToken { Name = "refresh_token", Value = "old-refresh" },
            new AuthenticationToken { Name = "id_token", Value = IdToken("alice", ["editor"]) },
            new AuthenticationToken { Name = "expires_at", Value = expiresAt.ToString("o", CultureInfo.InvariantCulture) },
        ]);
        return properties;
    }

    private static string Encoded(object value) => Base64UrlEncoder.Encode(JsonSerializer.SerializeToUtf8Bytes(value));

    public static OpenIdConnectOptions OpenIdOptions() => new()
    {
        ClientId = "cloudground-app-api",
        ClientSecret = "secret",
        ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(
            new OpenIdConnectConfiguration { TokenEndpoint = "https://identity.example/token" }),
    };
}
