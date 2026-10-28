using System.Globalization;
using System.Net;
using CloudGround.App.Bff.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace CloudGround.App.Bff.Tests;

public sealed class AccessTokenRefresherTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 28, 9, 0, 0, TimeSpan.Zero);

    private const string NewTokens = """{"access_token":"new-access","refresh_token":"new-refresh","expires_in":300}""";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Token_close_to_expiry_is_refreshed()
    {
        var endpoint = new StubTokenEndpoint(HttpStatusCode.OK, NewTokens);
        var properties = SessionExpiringAt(Now.AddSeconds(30));

        var result = await RefresherFor(endpoint).RefreshIfCloseToExpiryAsync(properties, Options(), Cancellation);

        Assert.Equal(TokenRefresh.Refreshed, result);
        Assert.Equal("new-access", properties.GetTokenValue("access_token"));
        Assert.Equal("new-refresh", properties.GetTokenValue("refresh_token"));
        Assert.Equal(Now.AddSeconds(300), DateTimeOffset.Parse(properties.GetTokenValue("expires_at")!, CultureInfo.InvariantCulture));
        Assert.Contains("grant_type=refresh_token", Assert.Single(endpoint.RequestBodies), StringComparison.Ordinal);
        Assert.Contains("refresh_token=old-refresh", endpoint.RequestBodies[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task Token_far_from_expiry_is_kept()
    {
        var endpoint = new StubTokenEndpoint(HttpStatusCode.OK, NewTokens);
        var properties = SessionExpiringAt(Now.AddMinutes(4));

        var result = await RefresherFor(endpoint).RefreshIfCloseToExpiryAsync(properties, Options(), Cancellation);

        Assert.Equal(TokenRefresh.NotNeeded, result);
        Assert.Equal("old-access", properties.GetTokenValue("access_token"));
        Assert.Empty(endpoint.RequestBodies);
    }

    [Fact]
    public async Task Failed_refresh_reports_failure()
    {
        var endpoint = new StubTokenEndpoint(HttpStatusCode.BadRequest, """{"error":"invalid_grant"}""");
        var properties = SessionExpiringAt(Now.AddSeconds(30));

        var result = await RefresherFor(endpoint).RefreshIfCloseToExpiryAsync(properties, Options(), Cancellation);

        Assert.Equal(TokenRefresh.Failed, result);
        Assert.Equal("old-access", properties.GetTokenValue("access_token"));
    }

    private static AccessTokenRefresher RefresherFor(StubTokenEndpoint endpoint) =>
        new(new HttpClient(endpoint), new FixedClock(Now));

    private static AuthenticationProperties SessionExpiringAt(DateTimeOffset expiresAt)
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

    private static OpenIdConnectOptions Options() => new()
    {
        ClientId = "cloudground-bff",
        ClientSecret = "secret",
        ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(
            new OpenIdConnectConfiguration { TokenEndpoint = "https://identity.example/token" }),
    };
}
