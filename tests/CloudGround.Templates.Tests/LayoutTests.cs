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
    [InlineData("src/Contoso.Shop.AppHost/Contoso.Shop.AppHost.csproj")]
    [InlineData("src/Contoso.Shop.ServiceDefaults/Contoso.Shop.ServiceDefaults.csproj")]
    [InlineData("src/Contoso.Shop.Api/Contoso.Shop.Api.csproj")]
    [InlineData("tests/Contoso.Shop.Integration.Tests/Contoso.Shop.Integration.Tests.csproj")]
    public async Task Generated_solution_has_root_file(string relativePath)
    {
        var solution = await package.GenerateAsync("Contoso.Shop");

        Assert.Contains(relativePath, solution.RelativeFiles());
    }
}
