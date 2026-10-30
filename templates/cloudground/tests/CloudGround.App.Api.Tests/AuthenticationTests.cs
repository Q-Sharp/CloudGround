namespace CloudGround.App.Api.Tests;

public sealed class AuthenticationTests
{
    private const string IdentityProviderUrl = "https://id.example.com";
    private const string Authority = "https://id.example.com/realms/cloudground-app";

    [Theory]
    [InlineData("Bearer eyJ", JwtBearerDefaults.AuthenticationScheme)]
    [InlineData("bearer eyJ", JwtBearerDefaults.AuthenticationScheme)]
    [InlineData("Basic abc", CookieAuthenticationDefaults.AuthenticationScheme)]
    [InlineData(null, CookieAuthenticationDefaults.AuthenticationScheme)]
    public void Bearer_tokens_authenticate_with_the_bearer_scheme_and_everything_else_with_the_session(string? authorization, string scheme)
    {
        using var services = Services();
        var request = new DefaultHttpContext();
        request.Request.Headers.Authorization = authorization;

        var options = services.GetRequiredService<IOptionsMonitor<PolicySchemeOptions>>().Get(Authentication.SessionOrBearerScheme);

        Assert.Equal(scheme, options.ForwardDefaultSelector!(request));
    }

    [Fact]
    public async Task Session_or_bearer_is_the_default_scheme()
    {
        using var services = Services();

        var defaultScheme = await services.GetRequiredService<IAuthenticationSchemeProvider>().GetDefaultAuthenticateSchemeAsync();

        Assert.Equal(Authentication.SessionOrBearerScheme, defaultScheme?.Name);
    }

    [Fact]
    public void Challenges_and_forbidden_answers_stay_status_codes()
    {
        using var services = Services();

        var options = services.GetRequiredService<IOptionsMonitor<PolicySchemeOptions>>().Get(Authentication.SessionOrBearerScheme);

        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, options.ForwardChallenge);
        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, options.ForwardForbid);
    }

    [Fact]
    public void Session_slides_and_refreshes_its_access_token()
    {
        using var services = Services();

        var cookie = services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(CookieAuthenticationDefaults.AuthenticationScheme);

        Assert.Equal(TimeSpan.FromHours(8), cookie.ExpireTimeSpan);
        Assert.True(cookie.SlidingExpiration);
        Assert.True(cookie.Cookie.HttpOnly);
        Assert.Equal(typeof(RefreshingCookieEvents), cookie.EventsType);
        Assert.IsType<DistributedTicketStore>(cookie.SessionStore);
    }

    [Fact]
    public void Session_and_sign_in_cookies_are_named_after_the_solution()
    {
        using var services = Services();

        var cookie = services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(CookieAuthenticationDefaults.AuthenticationScheme);
        var openIdConnect = services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get(OpenIdConnectDefaults.AuthenticationScheme);

        Assert.Equal("cloudground-app-session", cookie.Cookie.Name);
        Assert.Equal("cloudground-app-correlation.", openIdConnect.CorrelationCookie.Name);
        Assert.Equal("cloudground-app-nonce.", openIdConnect.NonceCookie.Name);
    }

    [Theory]
    [InlineData(IdentityProviderUrl)]
    [InlineData(IdentityProviderUrl + "/")]
    public void Sign_in_uses_the_configured_identity_provider_outside_development(string identityProviderUrl)
    {
        using var services = Services(identityProviderUrl);

        var options = services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get(OpenIdConnectDefaults.AuthenticationScheme);

        Assert.Equal(Authority, options.Authority);
        Assert.True(options.RequireHttpsMetadata);
        Assert.Equal("cloudground-app-api", options.ClientId);
        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, options.SignInScheme);
        Assert.Equal("code", options.ResponseType);
        Assert.True(options.UsePkce);
        Assert.True(options.SaveTokens);
    }

    [Theory]
    [InlineData(IdentityProviderUrl)]
    [InlineData(IdentityProviderUrl + "/")]
    public void Bearer_tokens_come_from_the_configured_identity_provider_outside_development(string identityProviderUrl)
    {
        using var services = Services(identityProviderUrl);

        var options = services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);

        Assert.Equal(Authority, options.Authority);
        Assert.True(options.RequireHttpsMetadata);
        Assert.Equal("cloudground-app-api", options.Audience);
    }

    private static ServiceProvider Services(string identityProviderUrl = IdentityProviderUrl)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Production });
        builder.Configuration["Keycloak:Url"] = identityProviderUrl;
        builder.Configuration["ConnectionStrings:cache"] = "127.0.0.1:1,connectTimeout=100";
        builder.AddRedisDistributedCache("cache");
        builder.AddApiAuthentication();
        return builder.Services.BuildServiceProvider();
    }
}
