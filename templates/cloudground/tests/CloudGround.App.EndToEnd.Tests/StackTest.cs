namespace CloudGround.App.EndToEnd.Tests;

public abstract class StackTest(StackFixture stack) : BrowserTest
{
    protected StackFixture Stack { get; } = stack;

    protected async Task<IPage> OpenAsync(string path, bool javaScriptEnabled = true)
    {
        var context = await NewContext(new() { BaseURL = Stack.BffAddress.ToString(), JavaScriptEnabled = javaScriptEnabled });
        var page = await context.NewPageAsync();
        await page.GotoAsync(path);
        return page;
    }

    protected async Task LogInAsync(IPage page, string user)
    {
        await page.GetByRole(AriaRole.Link, new() { Name = "Log in" }).ClickAsync();
        await SubmitCredentialsAsync(page, user);
        await Expect(page.Locator("[data-test=user]")).ToHaveTextAsync(user);
    }

    protected static async Task SubmitCredentialsAsync(IPage page, string user)
    {
        await page.Locator("#username").FillAsync(user);
        await page.Locator("#password").FillAsync(user);
        await page.Locator("#kc-login").ClickAsync();
    }
}
