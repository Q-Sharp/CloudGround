namespace CloudGround.App.Api;

internal static class XsrfProtection
{
    private const string HeaderName = "X-XSRF-TOKEN";
    private const string CookieName = "cloudground-app-xsrf";
    private const string AntiforgeryCookieName = "cloudground-app-antiforgery";

    public static WebApplicationBuilder AddXsrfProtection(this WebApplicationBuilder builder)
    {
        var secure = SecurePolicy(builder.Environment);
        builder.Services.AddAntiforgery(options =>
        {
            options.HeaderName = HeaderName;
            options.Cookie.Name = AntiforgeryCookieName;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = secure;
        });

        return builder;
    }

    public static IApplicationBuilder UseXsrfProtection(this IApplicationBuilder app)
    {
        var xsrfCookie = new CookieBuilder
        {
            Name = CookieName,
            HttpOnly = false,
            SameSite = SameSiteMode.Strict,
            SecurePolicy = SecurePolicy(app.ApplicationServices.GetRequiredService<IHostEnvironment>()),
            Path = "/",
        };

        return app.Use(async (context, next) =>
        {
            if (!IsSignedInWithTheSessionCookie(context))
            {
                await next(context);
                return;
            }

            var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
            if (IsUnsafe(context.Request) && !await antiforgery.IsRequestValidAsync(context))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context,
                    ProblemDetails = { Status = StatusCodes.Status400BadRequest, Title = "The request has no valid XSRF token." },
                });
                return;
            }

            var tokens = antiforgery.GetAndStoreTokens(context);
            context.Response.Cookies.Append(CookieName, tokens.RequestToken!, xsrfCookie.Build(context));

            await next(context);
        });
    }

    private static CookieSecurePolicy SecurePolicy(IHostEnvironment environment) =>
        environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;

    private static bool IsSignedInWithTheSessionCookie(HttpContext context) =>
        context.User.Identity?.IsAuthenticated == true && !Authentication.UsesBearerToken(context.Request);

    private static bool IsUnsafe(HttpRequest request) =>
        !HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method)
        && !HttpMethods.IsOptions(request.Method) && !HttpMethods.IsTrace(request.Method);
}
