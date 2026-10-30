namespace CloudGround.App.Integration.Tests;

public sealed class SessionRefreshTests(AppHostFixture fixture)
{
    private static readonly Uri UserUri = new("/account/user", UriKind.Relative);

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static string NewUserName() => $"user-{Guid.NewGuid():N}";

    [Fact]
    public async Task Role_taken_in_keycloak_ends_at_the_next_refresh()
    {
        await using var admin = new KeycloakAdmin(fixture.App);
        await admin.RefreshAccessTokensOnEveryRequestAsync();
        var name = NewUserName();
        var id = await admin.CreateEditorAsync(name);
        using var session = await BrowserLogin.LogInAsync(fixture, name);
        Assert.Contains("editor", await RolesAsync(session));

        await admin.TakeEditorRoleAsync(id);

        Assert.DoesNotContain("editor", await RolesAsync(session));
    }

    [Fact]
    public async Task User_blocked_in_keycloak_is_signed_out_at_the_next_refresh()
    {
        await using var admin = new KeycloakAdmin(fixture.App);
        await admin.RefreshAccessTokensOnEveryRequestAsync();
        var name = NewUserName();
        var id = await admin.CreateEditorAsync(name);
        using var session = await BrowserLogin.LogInAsync(fixture, name);
        Assert.Contains("editor", await RolesAsync(session));

        await admin.BlockAsync(id);

        using var response = await session.Client.GetAsync(UserUri, Cancellation);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<List<string?>> RolesAsync(BrowserSession session)
    {
        var user = await session.Client.GetFromJsonAsync<JsonElement>(UserUri, Cancellation);
        return [.. user.GetProperty("roles").EnumerateArray().Select(role => role.GetString())];
    }
}
