using System.Security.Claims;
using CloudGround.App.Bff.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CloudGround.App.Bff.Endpoints;

internal sealed record BffUser(string Name, IReadOnlyList<string> Roles);

internal static class BffEndpoints
{
    private const string Home = "/";

    public static IEndpointRouteBuilder MapBffEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var bff = endpoints.MapGroup("/bff");
        bff.MapGet("/login", LogIn);
        bff.MapPost("/logout", LogOut);
        bff.MapGet("/user", GetUser);
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

    private static Results<Ok<BffUser>, UnauthorizedHttpResult> GetUser(ClaimsPrincipal user) =>
        user.Identity is { IsAuthenticated: true, Name: { } name }
            ? TypedResults.Ok(new BffUser(name, [.. user.FindAll(BffAuthentication.RoleClaim).Select(role => role.Value)]))
            : TypedResults.Unauthorized();

    private static bool IsLocal(string? url) =>
        url is ['/', not '/' and not '\\', ..] or "/";
}
