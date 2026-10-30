using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CloudGround.App.Api;

internal sealed record AccountUser(string Name, IReadOnlyList<string> Roles);

internal static class AccountEndpoints
{
    private const string Home = "/";

    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var account = endpoints.MapGroup("/account").ExcludeFromDescription();
        account.MapGet("/login", LogIn);
        account.MapPost("/logout", LogOut);
        account.MapGet("/user", GetUser);
        return endpoints;
    }

    private static ChallengeHttpResult LogIn(string? returnUrl) =>
        TypedResults.Challenge(
            new AuthenticationProperties { RedirectUri = IsLocal(returnUrl) ? returnUrl : Home },
            [OpenIdConnectDefaults.AuthenticationScheme]);

    private static SignOutHttpResult LogOut() =>
        TypedResults.SignOut(
            new AuthenticationProperties { RedirectUri = Home },
            [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]);

    private static Results<Ok<AccountUser>, UnauthorizedHttpResult> GetUser(ClaimsPrincipal user) =>
        user.Identity is { IsAuthenticated: true, Name: { } name }
            ? TypedResults.Ok(new AccountUser(name, [.. user.FindAll(Authentication.RoleClaim).Select(role => role.Value)]))
            : TypedResults.Unauthorized();

    private static bool IsLocal(string? url) =>
        url is ['/', not '/' and not '\\', ..] or "/";
}
