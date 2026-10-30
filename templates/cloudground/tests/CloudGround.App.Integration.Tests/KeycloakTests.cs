using System.Net;
using System.Text.Json;
using System.Web;

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

    [Fact]
    public async Task Used_refresh_tokens_stay_valid()
    {
        var refreshToken = (await Tokens.GrantAsync(fixture.App, "alice", Cancellation)).GetProperty("refresh_token").GetString()!;

        var first = await Tokens.RefreshAsync(fixture.App, refreshToken, Cancellation);
        var second = await Tokens.RefreshAsync(fixture.App, refreshToken, Cancellation);

        Assert.True(first.TryGetProperty("access_token", out _));
        Assert.True(second.TryGetProperty("access_token", out _));
    }

    [Fact]
    public async Task Api_client_cannot_sign_in_without_pkce()
    {
        using var keycloak = new HttpClient(new SocketsHttpHandler { AllowAutoRedirect = false }) { BaseAddress = fixture.App.GetEndpoint("keycloak", "http") };
        var redirectUri = Uri.EscapeDataString("http://localhost/signin-oidc");

        using var response = await keycloak.GetAsync(
            new Uri($"/realms/{Tokens.Realm}/protocol/openid-connect/auth?client_id=cloudground-app-api&response_type=code&scope=openid&redirect_uri={redirectUri}", UriKind.Relative),
            Cancellation);

        var error = HttpUtility.ParseQueryString(response.Headers.Location?.Query ?? string.Empty);
        Assert.Equal("invalid_request", error["error"]);
        Assert.Contains("code_challenge_method", error["error_description"], StringComparison.Ordinal);
    }

    private static List<string> Audiences(JsonElement payload) =>
        payload.GetProperty("aud") is { ValueKind: JsonValueKind.Array } audiences
            ? [.. audiences.EnumerateArray().Select(audience => audience.GetString()!)]
            : [payload.GetProperty("aud").GetString()!];

    private static List<string> Roles(JsonElement payload) =>
        payload.TryGetProperty("roles", out var roles) ? [.. roles.EnumerateArray().Select(role => role.GetString()!)] : [];
}
