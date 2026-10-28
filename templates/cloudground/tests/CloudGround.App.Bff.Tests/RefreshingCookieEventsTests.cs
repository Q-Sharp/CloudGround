using System.Net;
using System.Security.Claims;
using CloudGround.App.Bff.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using static CloudGround.App.Bff.Tests.TokenSessions;

namespace CloudGround.App.Bff.Tests;

public sealed class RefreshingCookieEventsTests
{
    [Fact]
    public async Task Refreshed_session_is_renewed()
    {
        var (context, user) = SessionExpiringSoon();

        await EventsFor(new StubTokenEndpoint(HttpStatusCode.OK, NewTokens)).ValidatePrincipal(context);

        Assert.True(context.ShouldRenew);
        Assert.NotNull(context.Principal);
        Assert.False(user.SignedOut);
        Assert.Equal("new-access", context.Properties.GetTokenValue("access_token"));
    }

    [Fact]
    public async Task Failed_refresh_ends_the_session()
    {
        var (context, user) = SessionExpiringSoon();

        await EventsFor(new StubTokenEndpoint(HttpStatusCode.BadRequest, """{"error":"invalid_grant"}""")).ValidatePrincipal(context);

        Assert.Null(context.Principal);
        Assert.True(user.SignedOut);
    }

    private static RefreshingCookieEvents EventsFor(HttpMessageHandler endpoint) =>
        new(AccessTokenRefresherTests.RefresherFor(endpoint), new FixedOptionsMonitor<OpenIdConnectOptions>(Options()));

    private static (CookieValidatePrincipalContext Context, SignedInUser User) SessionExpiringSoon()
    {
        var user = new SignedInUser(accessToken: null);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddSingleton<IAuthenticationService>(user).BuildServiceProvider(),
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("preferred_username", "alice")], "test"));
        var ticket = new AuthenticationTicket(principal, ExpiringAt(Now.AddSeconds(30)), CookieAuthenticationDefaults.AuthenticationScheme);
        var scheme = new AuthenticationScheme(CookieAuthenticationDefaults.AuthenticationScheme, null, typeof(CookieAuthenticationHandler));
        return (new CookieValidatePrincipalContext(httpContext, scheme, new CookieAuthenticationOptions(), ticket), user);
    }
}
