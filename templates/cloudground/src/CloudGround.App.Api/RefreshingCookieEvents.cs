namespace CloudGround.App.Api;

internal sealed class RefreshingCookieEvents(AccessTokenRefresher refresher, IOptionsMonitor<OpenIdConnectOptions> openIdConnect) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var options = openIdConnect.Get(OpenIdConnectDefaults.AuthenticationScheme);
        switch (await refresher.RefreshIfCloseToExpiryAsync(context.Properties, options, context.HttpContext.RequestAborted))
        {
            case TokenRefresh.Refreshed:
                context.ReplacePrincipal(WithRolesOf(context.Properties.GetTokenValue("id_token")!, context.Principal!));
                context.ShouldRenew = true;
                break;
            case TokenRefresh.Failed:
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                break;
        }
    }

    private static ClaimsPrincipal WithRolesOf(string idToken, ClaimsPrincipal session)
    {
        var identity = ((ClaimsIdentity)session.Identity!).Clone();
        foreach (var role in identity.FindAll(Authentication.RoleClaim).ToList())
        {
            identity.RemoveClaim(role);
        }

        identity.AddClaims(new JsonWebToken(idToken).Claims.Where(claim => claim.Type == Authentication.RoleClaim));
        return new ClaimsPrincipal(identity);
    }
}
