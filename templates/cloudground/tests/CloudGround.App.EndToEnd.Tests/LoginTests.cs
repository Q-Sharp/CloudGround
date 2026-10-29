namespace CloudGround.App.EndToEnd.Tests;

[Collection(RunningApp.Name)]
public sealed class LoginTests(StackFixture stack) : StackTest(stack)
{
    [Fact]
    public async Task Login_and_logout_round_trip()
    {
        var page = await OpenAsync("/");
        await LogInAsync(page, "alice");

        await page.GetByRole(AriaRole.Button, new() { Name = "Log out" }).ClickAsync();

        await Expect(page).ToHaveURLAsync(Stack.BffAddress.ToString());
        await page.GetByRole(AriaRole.Link, new() { Name = "Log in" }).ClickAsync();
        await Expect(page.Locator("#kc-login")).ToBeVisibleAsync();
    }
}
