namespace CloudGround.App.Integration.Tests;

internal static class Tokens
{
    public const string Realm = "cloudground-app";

    private const string TestClientId = "cloudground-app-tests";

    public static async Task<string> ForAsync(DistributedApplication app, string user, CancellationToken cancellationToken) =>
        (await GrantAsync(app, user, cancellationToken)).GetProperty("access_token").GetString()!;

    public static Task<JsonElement> GrantAsync(DistributedApplication app, string user, CancellationToken cancellationToken) =>
        RequestAsync(app, new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = TestClientId,
            ["username"] = user,
            ["password"] = user,
            ["scope"] = "openid",
        }, cancellationToken);

    public static Task<JsonElement> RefreshAsync(DistributedApplication app, string refreshToken, CancellationToken cancellationToken) =>
        RequestAsync(app, new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = TestClientId,
            ["refresh_token"] = refreshToken,
        }, cancellationToken);

    private static async Task<JsonElement> RequestAsync(DistributedApplication app, Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        using var keycloak = new HttpClient { BaseAddress = app.GetEndpoint("keycloak", "http") };
        using var request = new FormUrlEncodedContent(form);
        using var response = await keycloak.PostAsync(new Uri($"/realms/{Realm}/protocol/openid-connect/token", UriKind.Relative), request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
    }

    public static JsonElement PayloadOf(string token)
    {
        var payload = token.Split('.')[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
        return JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload))).RootElement;
    }
}
