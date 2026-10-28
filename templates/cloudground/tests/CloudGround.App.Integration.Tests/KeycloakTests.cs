using System.Net;
using System.Text.Json;

namespace CloudGround.App.Integration.Tests;

public sealed class KeycloakTests(AppHostFixture fixture)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Realm_publishes_its_openid_configuration()
    {
        using var keycloak = new HttpClient { BaseAddress = fixture.App.GetEndpoint("keycloak", "http") };

        using var response = await keycloak.GetAsync(new Uri($"/realms/{Tokens.Realm}/.well-known/openid-configuration", UriKind.Relative), Cancellation);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Alice_gets_a_token_with_the_editor_role_for_the_api()
    {
        var payload = Tokens.PayloadOf(await Tokens.ForAsync(fixture.App, "alice", Cancellation));

        Assert.Contains("cloudground-app-api", Audiences(payload));
        Assert.Contains("editor", Roles(payload));
    }

    [Fact]
    public async Task Bob_gets_a_token_without_the_editor_role()
    {
        var payload = Tokens.PayloadOf(await Tokens.ForAsync(fixture.App, "bob", Cancellation));

        Assert.Contains("cloudground-app-api", Audiences(payload));
        Assert.DoesNotContain("editor", Roles(payload));
    }

    private static List<string> Audiences(JsonElement payload) =>
        payload.GetProperty("aud") is { ValueKind: JsonValueKind.Array } audiences
            ? [.. audiences.EnumerateArray().Select(audience => audience.GetString()!)]
            : [payload.GetProperty("aud").GetString()!];

    private static List<string> Roles(JsonElement payload) =>
        payload.TryGetProperty("roles", out var roles) ? [.. roles.EnumerateArray().Select(role => role.GetString()!)] : [];
}
