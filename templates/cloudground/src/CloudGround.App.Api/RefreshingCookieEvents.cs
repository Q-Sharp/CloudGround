using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace CloudGround.App.Api;

internal sealed class RefreshingCookieEvents(AccessTokenRefresher refresher, IOptionsMonitor<OpenIdConnectOptions> openIdConnect) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var options = openIdConnect.Get(OpenIdConnectDefaults.AuthenticationScheme);
        switch (await refresher.RefreshIfCloseToExpiryAsync(context.Properties, options, context.HttpContext.RequestAborted))
        {
            case TokenRefresh.Refreshed:
                context.ShouldRenew = true;
                break;
            case TokenRefresh.Failed:
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                break;
        }
    }
}
