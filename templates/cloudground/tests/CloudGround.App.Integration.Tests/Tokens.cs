using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace CloudGround.App.Integration.Tests;

internal static class Tokens
{
    public const string Realm = "cloudground";

    private const string TestClientId = "cloudground-tests";

    public static async Task<string> ForAsync(DistributedApplication app, string user, CancellationToken cancellationToken)
    {
        using var keycloak = new HttpClient { BaseAddress = app.GetEndpoint("keycloak", "http") };
        using var request = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = TestClientId,
            ["username"] = user,
            ["password"] = user,
            ["scope"] = "openid",
        });

        using var response = await keycloak.PostAsync(new Uri($"/realms/{Realm}/protocol/openid-connect/token", UriKind.Relative), request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        return token.GetProperty("access_token").GetString()!;
    }

    public static JsonElement PayloadOf(string token)
    {
        var payload = token.Split('.')[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
        return JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload))).RootElement;
    }
}
