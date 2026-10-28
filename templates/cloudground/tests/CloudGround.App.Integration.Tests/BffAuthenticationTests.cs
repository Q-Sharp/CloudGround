using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CloudGround.App.Integration.Tests;

public sealed class BffAuthenticationTests(AppHostFixture fixture)
{
    private static readonly Uri UserUri = new("/bff/user", UriKind.Relative);

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Anonymous_user_endpoint_returns_401()
    {
        using var client = fixture.App.CreateHttpClient("bff", "http");

        using var response = await client.GetAsync(UserUri, Cancellation);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task User_endpoint_returns_name_and_roles_only()
    {
        using var session = await BffLogin.LogInAsync(fixture, "alice");

        var user = await session.Client.GetFromJsonAsync<JsonElement>(UserUri, Cancellation);

        Assert.Equal(["name", "roles"], user.EnumerateObject().Select(property => property.Name).Order());
        Assert.Equal("alice", user.GetProperty("name").GetString());
        Assert.Contains("editor", user.GetProperty("roles").EnumerateArray().Select(role => role.GetString()));
    }

    [Fact]
    public async Task Login_ignores_foreign_return_urls()
    {
        using var session = await BffLogin.LogInAsync(fixture, "alice", returnUrl: "https://evil.example/");

        Assert.Equal(session.Client.BaseAddress!.Authority, session.LandingPage.Authority);
        Assert.Equal("/", session.LandingPage.AbsolutePath);
    }

    [Fact]
    public async Task Logout_ends_the_session()
    {
        using var session = await BffLogin.LogInAsync(fixture, "alice");
        using var logout = new HttpRequestMessage(HttpMethod.Post, new Uri("/bff/logout", UriKind.Relative));
        logout.Headers.Add("X-XSRF-TOKEN", session.XsrfToken);

        using var loggedOut = await session.Client.SendAsync(logout, Cancellation);
        using var response = await session.Client.GetAsync(UserUri, Cancellation);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
