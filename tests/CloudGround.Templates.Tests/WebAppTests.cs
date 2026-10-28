namespace CloudGround.Templates.Tests;

[Collection(SharedTemplatePackage.Name)]
public sealed class WebAppTests(TemplatePackage package)
{
    private static readonly string[][] Steps = [["ci"], ["run", "build"], ["test"]];

    [Fact]
    public async Task Generated_web_app_lints_builds_and_tests()
    {
        var solution = await package.GenerateAsync("Contoso.Shop");
        var web = Path.Combine(solution.Root, "src", "Contoso.Shop.Web");

        foreach (var step in Steps)
        {
            var result = await ProcessRunner.RunAsync("npm", web, step);
            Assert.True(result.ExitCode == 0, $"npm {string.Join(' ', step)}: {result.Output}");
        }
    }
}
