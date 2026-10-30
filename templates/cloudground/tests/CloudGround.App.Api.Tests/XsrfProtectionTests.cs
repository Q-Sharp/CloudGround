using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CloudGround.App.Api.Tests;

public sealed class XsrfProtectionTests
{
    [Fact]
    public async Task Signed_in_requests_get_a_readable_xsrf_cookie_named_after_the_solution()
    {
        await using var app = App();
        var pipeline = new ApplicationBuilder(app.Services).UseXsrfProtection().Build();
        var context = new DefaultHttpContext
        {
            RequestServices = app.Services,
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("preferred_username", "alice")], "test")),
        };
        context.Request.Method = HttpMethods.Get;
        context.Request.Scheme = "https";

        await pipeline(context);

        var cookie = Assert.Single(context.Response.Headers.SetCookie, header => header!.StartsWith("cloudground-app-xsrf=", StringComparison.Ordinal))!;
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("httponly", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Antiforgery_cookie_is_named_after_the_solution_and_needs_https_outside_development()
    {
        await using var app = App();

        var antiforgery = app.Services.GetRequiredService<IOptions<AntiforgeryOptions>>().Value;

        Assert.Equal("cloudground-app-antiforgery", antiforgery.Cookie.Name);
        Assert.Equal(CookieSecurePolicy.Always, antiforgery.Cookie.SecurePolicy);
    }

    private static WebApplication App()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Production });
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.AddXsrfProtection();
        return builder.Build();
    }
}
