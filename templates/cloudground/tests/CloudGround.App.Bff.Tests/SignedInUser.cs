using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace CloudGround.App.Bff.Tests;

internal sealed class SignedInUser(string? accessToken) : IAuthenticationService
{
    public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
    {
        if (accessToken is null)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var properties = new AuthenticationProperties();
        properties.StoreTokens([new AuthenticationToken { Name = "access_token", Value = accessToken }]);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("preferred_username", "alice")], "test"));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, properties, "test")));
    }

    public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;

    public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;

    public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties) => Task.CompletedTask;

    public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
}
