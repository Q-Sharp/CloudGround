namespace CloudGround.App.Api.Tests;

public sealed class RefreshingCookieEventsTests
{
    [Fact]
    public async Task Refreshed_session_is_renewed()
    {
        var (context, authentication) = SessionExpiringSoon();

        await EventsFor(new StubTokenEndpoint(HttpStatusCode.OK, NewTokens)).ValidatePrincipal(context);

        Assert.True(context.ShouldRenew);
        Assert.NotNull(context.Principal);
        Assert.False(authentication.SignedOut);
        Assert.Equal("new-access", context.Properties.GetTokenValue("access_token"));
    }

    [Fact]
    public async Task Refreshed_session_holds_the_roles_of_the_new_id_token()
    {
        var (context, _) = SessionExpiringSoon();

        await EventsFor(new StubTokenEndpoint(HttpStatusCode.OK, TokensWithRoles("reader"))).ValidatePrincipal(context);

        Assert.Equal(["reader"], context.Principal!.FindAll(Authentication.RoleClaim).Select(role => role.Value));
        Assert.Equal("alice", context.Principal.FindFirst("preferred_username")?.Value);
    }

    [Fact]
    public async Task Session_without_roles_in_the_new_id_token_holds_none()
    {
        var (context, _) = SessionExpiringSoon();

        await EventsFor(new StubTokenEndpoint(HttpStatusCode.OK, TokensWithRoles())).ValidatePrincipal(context);

        Assert.Empty(context.Principal!.FindAll(Authentication.RoleClaim));
    }

    [Fact]
    public async Task Refresh_without_an_id_token_ends_the_session()
    {
        var (context, authentication) = SessionExpiringSoon();

        await EventsFor(new StubTokenEndpoint(HttpStatusCode.OK, TokensWithoutIdToken)).ValidatePrincipal(context);

        Assert.Null(context.Principal);
        Assert.True(authentication.SignedOut);
    }

    [Fact]
    public async Task Failed_refresh_ends_the_session()
    {
        var (context, authentication) = SessionExpiringSoon();

        await EventsFor(new StubTokenEndpoint(HttpStatusCode.BadRequest, """{"error":"invalid_grant"}""")).ValidatePrincipal(context);

        Assert.Null(context.Principal);
        Assert.True(authentication.SignedOut);
    }

    private static RefreshingCookieEvents EventsFor(HttpMessageHandler endpoint) =>
        new(AccessTokenRefresherTests.RefresherFor(endpoint), new FixedOptionsMonitor<OpenIdConnectOptions>(OpenIdOptions()));

    private static (CookieValidatePrincipalContext Context, SignOutRecorder Authentication) SessionExpiringSoon()
    {
        var authentication = new SignOutRecorder();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddSingleton<IAuthenticationService>(authentication).BuildServiceProvider(),
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("preferred_username", "alice"), new Claim(Authentication.RoleClaim, "editor")], "test"));
        var ticket = new AuthenticationTicket(principal, ExpiringAt(Now.AddSeconds(30)), CookieAuthenticationDefaults.AuthenticationScheme);
        var scheme = new AuthenticationScheme(CookieAuthenticationDefaults.AuthenticationScheme, null, typeof(CookieAuthenticationHandler));
        return (new CookieValidatePrincipalContext(httpContext, scheme, new CookieAuthenticationOptions(), ticket), authentication);
    }
}
