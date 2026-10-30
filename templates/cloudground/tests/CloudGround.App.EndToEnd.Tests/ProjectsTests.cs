namespace CloudGround.App.EndToEnd.Tests;

[Collection(RunningApp.Name)]
public sealed class ProjectsTests(StackFixture stack) : StackTest(stack)
{
    [Fact]
    public async Task Public_page_shows_projects_without_javascript()
    {
        var name = NewProjectName();
        var editor = await OpenAsync("/projects");
        await LogInAsync(editor, "alice");
        await CreateProjectAsync(editor, name);

        var visitor = await OpenAsync("/explore", javaScriptEnabled: false);

        await Expect(visitor.GetByText(name)).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Explore_page_is_not_fetched_again_after_hydration()
    {
        var page = await NewPageAsync();
        var requestedPaths = new List<string>();
        page.Request += (_, request) => requestedPaths.Add(new Uri(request.Url).AbsolutePath);
        var hydrated = page.WaitForRequestAsync(request => new Uri(request.Url).AbsolutePath == "/account/user");

        await page.GotoAsync("/explore");
        await hydrated;
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        Assert.DoesNotContain(requestedPaths, path => path.StartsWith("/api/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Alice_creates_a_project_in_the_browser()
    {
        var name = NewProjectName();
        var page = await OpenAsync("/projects");
        await LogInAsync(page, "alice");

        await CreateProjectAsync(page, name);

        await page.ReloadAsync();
        await Expect(page.GetByText(name)).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Bob_cannot_create_projects()
    {
        var page = await OpenAsync("/projects");

        await LogInAsync(page, "bob");

        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "My projects" })).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Create" })).ToBeHiddenAsync();
    }

    private async Task CreateProjectAsync(IPage page, string name)
    {
        await page.GetByLabel("Name").FillAsync(name);
        await page.GetByRole(AriaRole.Button, new() { Name = "Create" }).ClickAsync();
        await Expect(page.GetByText(name)).ToBeVisibleAsync();
    }

    private static string NewProjectName() => $"Project {Guid.NewGuid():N}";
}
