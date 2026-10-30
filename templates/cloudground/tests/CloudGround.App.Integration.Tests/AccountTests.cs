using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;

namespace CloudGround.App.Integration.Tests;

public sealed class AccountTests(AppHostFixture fixture)
{
    private static readonly Uri UserUri = new("/account/user", UriKind.Relative);
    private static readonly Uri LogoutUri = new("/account/logout", UriKind.Relative);

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Anonymous_user_endpoint_returns_401()
    {
        using var client = fixture.CreateApiClient();

        using var response = await client.GetAsync(UserUri, Cancellation);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Login_redirects_to_keycloak_with_a_pushed_authorization_request()
    {
        using var client = new HttpClient(new SocketsHttpHandler { AllowAutoRedirect = false }) { BaseAddress = fixture.App.GetEndpoint("api", "http") };

        using var response = await client.GetAsync(new Uri("/account/login", UriKind.Relative), Cancellation);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!;
        var query = HttpUtility.ParseQueryString(location.Query);
        Assert.Equal(fixture.App.GetEndpoint("keycloak", "http").Authority, location.Authority);
        Assert.Equal($"/realms/{Tokens.Realm}/protocol/openid-connect/auth", location.AbsolutePath);
        Assert.Equal("cloudground-app-api", query["client_id"]);
        Assert.StartsWith("urn:ietf:params:oauth:request_uri:", query["request_uri"], StringComparison.Ordinal);
    }

    [Fact]
    public async Task User_endpoint_returns_name_and_roles_only()
    {
        using var session = await BrowserLogin.LogInAsync(fixture, "alice");

        var user = await session.Client.GetFromJsonAsync<JsonElement>(UserUri, Cancellation);

        Assert.Equal(["name", "roles"], user.EnumerateObject().Select(property => property.Name).Order());
        Assert.Equal("alice", user.GetProperty("name").GetString());
        Assert.Contains("editor", user.GetProperty("roles").EnumerateArray().Select(role => role.GetString()));
    }

    [Theory]
    [InlineData("https://evil.example/")]
    [InlineData("//evil.example")]
    [InlineData("/\\evil.example")]
    [InlineData("/\t/evil.example")]
    [InlineData("/\n/evil.example")]
    public async Task Login_ignores_foreign_return_urls(string returnUrl)
    {
        using var session = await BrowserLogin.LogInAsync(fixture, "alice", returnUrl);

        Assert.Equal(session.Client.BaseAddress!.Authority, session.LandingPage.Authority);
        Assert.Equal("/", session.LandingPage.AbsolutePath);
    }

    [Fact]
    public async Task Logout_ends_the_session()
    {
        using var session = await BrowserLogin.LogInAsync(fixture, "alice");
        using var logout = new HttpRequestMessage(HttpMethod.Post, LogoutUri);
        logout.Headers.Add("X-XSRF-TOKEN", session.XsrfToken);

        using var loggedOut = await session.Client.SendAsync(logout, Cancellation);
        using var response = await session.Client.GetAsync(UserUri, Cancellation);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_without_xsrf_token_keeps_the_session()
    {
        using var session = await BrowserLogin.LogInAsync(fixture, "alice");

        using var logout = await session.Client.PostAsync(LogoutUri, content: null, Cancellation);
        using var response = await session.Client.GetAsync(UserUri, Cancellation);

        Assert.Equal(HttpStatusCode.BadRequest, logout.StatusCode);
        Assert.Equal("application/problem+json", logout.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_account_route_returns_problem_details()
    {
        using var client = fixture.CreateApiClient();

        using var response = await client.GetAsync(new Uri("/account/does-not-exist", UriKind.Relative), Cancellation);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
