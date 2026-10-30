namespace CloudGround.App.Api.Tests;

public sealed class SessionStoreOutageTests
{
    private const string SessionCookie = "cloudground-app-session";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(CacheOutage.Unreachable)]
    [InlineData(CacheOutage.TimedOut)]
    public async Task Session_reads_as_anonymous_while_the_cache_is_down_and_works_again_afterwards(CacheOutage outage)
    {
        var cache = new SwitchableCache();
        await using var app = await StartAsync(cache);
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
        using var signIn = await client.PostAsync(new Uri("/sign-in", UriKind.Relative), content: null, Cancellation);
        var session = signIn.Headers.GetValues("Set-Cookie").Single(cookie => cookie.StartsWith($"{SessionCookie}=", StringComparison.Ordinal)).Split(';')[0];

        cache.Outage = outage;
        using var signedInPageDuringOutage = await GetAsync(client, "/signed-in", session);
        using var publicPageDuringOutage = await GetAsync(client, "/public", session);
        cache.Outage = CacheOutage.None;
        using var signedInPageAfterOutage = await GetAsync(client, "/signed-in", session);

        Assert.Equal(HttpStatusCode.Unauthorized, signedInPageDuringOutage.StatusCode);
        Assert.False(signedInPageDuringOutage.Headers.Contains("Set-Cookie"));
        Assert.Equal(HttpStatusCode.OK, publicPageDuringOutage.StatusCode);
        Assert.Equal(HttpStatusCode.OK, signedInPageAfterOutage.StatusCode);
    }

    private static async Task<WebApplication> StartAsync(IDistributedCache cache)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Configuration["Keycloak:Url"] = "http://127.0.0.1:1";
        builder.AddApiAuthentication();
        builder.Services.AddSingleton(cache);
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapPost("/sign-in", (HttpContext context) => context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, Alice(), WithTokens()));
        app.MapGet("/signed-in", () => TypedResults.Ok()).RequireAuthorization();
        app.MapGet("/public", () => TypedResults.Ok());
        await app.StartAsync(Cancellation);
        return app;
    }

    private static Task<HttpResponseMessage> GetAsync(HttpClient client, string path, string session)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
        request.Headers.Add("Cookie", session);
        return client.SendAsync(request, Cancellation);
    }

    private static ClaimsPrincipal Alice() =>
        new(new ClaimsIdentity([new Claim("preferred_username", "alice")], "test", "preferred_username", "roles"));

    private static AuthenticationProperties WithTokens()
    {
        var properties = new AuthenticationProperties();
        properties.StoreTokens(
        [
            new AuthenticationToken { Name = "access_token", Value = "access" },
            new AuthenticationToken { Name = "refresh_token", Value = "refresh" },
            new AuthenticationToken { Name = "expires_at", Value = DateTimeOffset.UtcNow.AddHours(1).ToString("o", CultureInfo.InvariantCulture) },
        ]);
        return properties;
    }
}
