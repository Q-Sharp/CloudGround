using System.Globalization;
using System.Net;
using CloudGround.App.Bff.Authentication;
using Microsoft.AspNetCore.Authentication;
using static CloudGround.App.Bff.Tests.TokenSessions;

namespace CloudGround.App.Bff.Tests;

public sealed class AccessTokenRefresherTests
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Token_close_to_expiry_is_refreshed()
    {
        var endpoint = new StubTokenEndpoint(HttpStatusCode.OK, NewTokens);
        var properties = ExpiringAt(Now.AddSeconds(30));

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
        var properties = ExpiringAt(Now.AddMinutes(4));

        var result = await RefresherFor(endpoint).RefreshIfCloseToExpiryAsync(properties, Options(), Cancellation);

        Assert.Equal(TokenRefresh.NotNeeded, result);
        Assert.Equal("old-access", properties.GetTokenValue("access_token"));
        Assert.Empty(endpoint.RequestBodies);
    }

    [Fact]
    public async Task Rejected_refresh_token_reports_failure()
    {
        var endpoint = new StubTokenEndpoint(HttpStatusCode.BadRequest, """{"error":"invalid_grant"}""");
        var properties = ExpiringAt(Now.AddSeconds(30));

        var result = await RefresherFor(endpoint).RefreshIfCloseToExpiryAsync(properties, Options(), Cancellation);

        Assert.Equal(TokenRefresh.Failed, result);
        Assert.Equal("old-access", properties.GetTokenValue("access_token"));
    }

    [Fact]
    public async Task Unreachable_identity_provider_keeps_a_still_valid_token()
    {
        var properties = ExpiringAt(Now.AddSeconds(30));

        var result = await RefresherFor(new UnreachableTokenEndpoint()).RefreshIfCloseToExpiryAsync(properties, Options(), Cancellation);

        Assert.Equal(TokenRefresh.NotNeeded, result);
        Assert.Equal("old-access", properties.GetTokenValue("access_token"));
    }

    [Fact]
    public async Task Server_error_keeps_a_still_valid_token()
    {
        var properties = ExpiringAt(Now.AddSeconds(30));

        var result = await RefresherFor(new StubTokenEndpoint(HttpStatusCode.ServiceUnavailable, "{}")).RefreshIfCloseToExpiryAsync(properties, Options(), Cancellation);

        Assert.Equal(TokenRefresh.NotNeeded, result);
        Assert.Equal("old-access", properties.GetTokenValue("access_token"));
    }

    [Fact]
    public async Task Unreachable_identity_provider_after_expiry_reports_failure()
    {
        var properties = ExpiringAt(Now.AddSeconds(-10));

        var result = await RefresherFor(new UnreachableTokenEndpoint()).RefreshIfCloseToExpiryAsync(properties, Options(), Cancellation);

        Assert.Equal(TokenRefresh.Failed, result);
    }

    internal static AccessTokenRefresher RefresherFor(HttpMessageHandler endpoint) =>
        new(new HttpClient(endpoint), new FixedClock(Now));
}
