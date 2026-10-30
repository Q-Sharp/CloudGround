using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.StackExchangeRedis;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using StackExchange.Redis;

namespace CloudGround.App.Api;

internal static class Authentication
{
    public const string SessionOrBearerScheme = "SessionOrBearer";
    public const string RoleClaim = "roles";

    private const string NameClaim = "preferred_username";
    private const string IdentityProviderName = "keycloak";
    private const string Realm = "cloudground-app";
    private const string IdentityProviderUrlSetting = "Keycloak:Url";
    private const string ClientSecretSetting = "Keycloak:ClientSecret";
    private const string ClientId = "cloudground-app-api";
    private const string Audience = "cloudground-app-api";
    private const string BearerPrefix = "Bearer ";
    private const string SessionCookieName = "session";
    private const string ApplicationName = "CloudGround.App.Api";
    private const string DataProtectionKeys = "data-protection-keys";
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);

    public static WebApplicationBuilder AddApiAuthentication(this WebApplicationBuilder builder)
    {
        var development = builder.Environment.IsDevelopment();
        var cookieSecurity = development ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        var authority = builder.Configuration[IdentityProviderUrlSetting] is { } identityProviderUrl
            ? $"{identityProviderUrl.TrimEnd('/')}/realms/{Realm}"
            : null;

        builder.Services.AddDataProtection().SetApplicationName(ApplicationName);
        builder.Services.AddOptions<KeyManagementOptions>()
            .Configure<IConnectionMultiplexer>((options, redis) => options.XmlRepository = new RedisXmlRepository(() => redis.GetDatabase(), DataProtectionKeys));

        builder.Services.AddSingleton<ITicketStore, DistributedTicketStore>();
        builder.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<ITicketStore>((options, store) => options.SessionStore = store);

        builder.Services.AddAuthentication(SessionOrBearerScheme)
            .AddPolicyScheme(SessionOrBearerScheme, displayName: null, options =>
            {
                options.ForwardDefaultSelector = context => UsesBearerToken(context.Request)
                    ? JwtBearerDefaults.AuthenticationScheme
                    : CookieAuthenticationDefaults.AuthenticationScheme;
                options.ForwardChallenge = JwtBearerDefaults.AuthenticationScheme;
                options.ForwardForbid = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.Name = SessionCookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = cookieSecurity;
                options.ExpireTimeSpan = SessionLifetime;
                options.SlidingExpiration = false;
            })
            .AddKeycloakOpenIdConnect(IdentityProviderName, Realm, options =>
            {
                options.ClientId = ClientId;
                options.ClientSecret = builder.Configuration[ClientSecretSetting];
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.ResponseMode = OpenIdConnectResponseMode.Query;
                options.SaveTokens = true;
                options.MapInboundClaims = false;
                options.RequireHttpsMetadata = !development;
                if (authority is not null)
                {
                    options.Authority = authority;
                }

                options.TokenValidationParameters.NameClaimType = NameClaim;
                options.TokenValidationParameters.RoleClaimType = RoleClaim;
                options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                options.CorrelationCookie.SecurePolicy = cookieSecurity;
                options.NonceCookie.SameSite = SameSiteMode.Lax;
                options.NonceCookie.SecurePolicy = cookieSecurity;
            })
            .AddKeycloakJwtBearer(IdentityProviderName, Realm, options =>
            {
                options.Audience = Audience;
                options.RequireHttpsMetadata = !development;
                if (authority is not null)
                {
                    options.Authority = authority;
                }

                options.MapInboundClaims = false;
                options.TokenValidationParameters.RoleClaimType = RoleClaim;
                options.TokenValidationParameters.NameClaimType = NameClaim;
            });
        builder.Services.AddAuthorization();

        return builder;
    }

    public static bool UsesBearerToken(HttpRequest request) =>
        request.Headers.Authorization.ToString().StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase);
}
