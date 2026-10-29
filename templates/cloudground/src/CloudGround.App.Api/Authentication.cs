namespace CloudGround.App.Api;

internal static class Authentication
{
    private const string IdentityProviderName = "keycloak";
    private const string Realm = "cloudground-app";
    private const string IdentityProviderUrlSetting = "Keycloak:Url";
    private const string Audience = "cloudground-app-api";
    private const string RoleClaim = "roles";
    private const string NameClaim = "preferred_username";

    public static WebApplicationBuilder AddApiAuthentication(this WebApplicationBuilder builder)
    {
        builder.Services.AddAuthentication()
            .AddKeycloakJwtBearer(IdentityProviderName, Realm, options =>
            {
                options.Audience = Audience;
                options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
                if (builder.Configuration[IdentityProviderUrlSetting] is { } identityProviderUrl)
                {
                    options.Authority = $"{identityProviderUrl}/realms/{Realm}";
                }

                options.MapInboundClaims = false;
                options.TokenValidationParameters.RoleClaimType = RoleClaim;
                options.TokenValidationParameters.NameClaimType = NameClaim;
            });
        builder.Services.AddAuthorization();

        return builder;
    }
}
