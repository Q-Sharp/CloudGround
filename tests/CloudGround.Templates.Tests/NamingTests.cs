namespace CloudGround.Templates.Tests;

[Collection(SharedTemplatePackage.Name)]
public sealed class NamingTests(TemplatePackage package)
{
    private static readonly string[] SourceNameForms = ["cloudground.app", "cloudground_app"];

    [Fact]
    public async Task Generated_solution_contains_no_source_name_form()
    {
        var solution = await package.GenerateAsync("Contoso.Shop");
        var files = solution.RelativeFiles();

        Assert.DoesNotContain(files, ContainsSourceNameForm);
        Assert.All(files, file => Assert.False(ContainsSourceNameForm(File.ReadAllText(Path.Combine(solution.Root, file))), file));
    }

    [Fact]
    public async Task Generated_solution_builds_and_its_tests_pass()
    {
        var solution = await package.GenerateAsync("Contoso.Shop");

        var build = await DotnetCli.RunAsync(solution.Root, "build");
        Assert.True(build.ExitCode == 0, build.Output);

        var test = await DotnetCli.RunAsync(solution.Root, "test", "--solution", "Contoso.Shop.slnx");
        Assert.True(test.ExitCode == 0, test.Output);
    }

    [Fact]
    public async Task Hyphenated_lowercase_name_builds()
    {
        var solution = await package.GenerateAsync("my-shop");

        Assert.Contains("src/my_shop.Api/Program.cs", solution.RelativeFiles());

        var build = await DotnetCli.RunAsync(solution.Root, "build");
        Assert.True(build.ExitCode == 0, build.Output);
    }

    private static bool ContainsSourceNameForm(string text) =>
        SourceNameForms.Any(form => text.Contains(form, StringComparison.OrdinalIgnoreCase));
}
