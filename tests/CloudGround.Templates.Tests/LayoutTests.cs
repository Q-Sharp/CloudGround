namespace CloudGround.Templates.Tests;

[Collection(SharedTemplatePackage.Name)]
public sealed class LayoutTests(TemplatePackage package)
{
    [Theory]
    [InlineData("Contoso.Shop.slnx")]
    [InlineData("global.json")]
    [InlineData("Directory.Build.props")]
    [InlineData("Directory.Packages.props")]
    [InlineData(".editorconfig")]
    [InlineData(".gitignore")]
    public async Task Generated_solution_has_root_file(string relativePath)
    {
        var solution = await package.GenerateAsync("Contoso.Shop");

        Assert.Contains(relativePath, solution.RelativeFiles());
    }
}
