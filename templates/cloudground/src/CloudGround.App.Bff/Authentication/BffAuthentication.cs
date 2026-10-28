using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace CloudGround.App.Bff.Authentication;

internal static class BffAuthentication
{
    public const string RoleClaim = "roles";
    public const string NameClaim = "preferred_username";

    private const string IdentityProviderName = "keycloak";
    private const string Realm = "cloudground";
    private const string ClientId = "cloudground-bff";
    private const string ClientSecretSetting = "Bff:ClientSecret";
    private const string SessionCookieName = "bff";
    private const string ApplicationName = "CloudGround.App.Bff";
    private const string DataProtectionKeys = "bff/data-protection-keys";
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);

    public static WebApplicationBuilder AddBffAuthentication(this WebApplicationBuilder builder)
    {
        var development = builder.Environment.IsDevelopment();
        var cookieSecurity = development ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;

        builder.Services.AddDataProtection().SetApplicationName(ApplicationName);
        builder.Services.AddOptions<KeyManagementOptions>()
            .Configure<IConnectionMultiplexer>((options, redis) => options.XmlRepository = new RedisXmlRepository(() => redis.GetDatabase(), DataProtectionKeys));

        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<ITicketStore, DistributedTicketStore>();
        builder.Services.AddHttpClient<AccessTokenRefresher>();
        builder.Services.AddScoped<RefreshingCookieEvents>();
        builder.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<ITicketStore>((options, store) => options.SessionStore = store);

        builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.Name = SessionCookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = cookieSecurity;
                options.ExpireTimeSpan = SessionLifetime;
                options.SlidingExpiration = true;
                options.EventsType = typeof(RefreshingCookieEvents);
            })
            .AddKeycloakOpenIdConnect(IdentityProviderName, Realm, options =>
            {
                options.ClientId = ClientId;
                options.ClientSecret = builder.Configuration[ClientSecretSetting];
                options.ResponseType = "code";
                options.ResponseMode = "query";
                options.SaveTokens = true;
                options.MapInboundClaims = false;
                options.RequireHttpsMetadata = !development;
                options.TokenValidationParameters.NameClaimType = NameClaim;
                options.TokenValidationParameters.RoleClaimType = RoleClaim;
                options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                options.CorrelationCookie.SecurePolicy = cookieSecurity;
                options.NonceCookie.SameSite = SameSiteMode.Lax;
                options.NonceCookie.SecurePolicy = cookieSecurity;
            });
        builder.Services.AddAuthorization();

        return builder;
    }
}
