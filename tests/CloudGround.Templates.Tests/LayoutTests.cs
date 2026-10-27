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
    [InlineData(".github/actions/build-and-test/action.yml")]
    [InlineData(".github/workflows/ci.yml")]
    [InlineData(".github/workflows/pr-title.yml")]
    [InlineData(".github/workflows/release-please.yml")]
    [InlineData("release-please-config.json")]
    [InlineData(".release-please-manifest.json")]
    [InlineData(".github/dependabot.yml")]
    [InlineData(".github/pull_request_template.md")]
    [InlineData("README.md")]
    public async Task Generated_solution_has_root_file(string relativePath)
    {
        var solution = await package.GenerateAsync("Contoso.Shop");

        Assert.Contains(relativePath, solution.RelativeFiles());
    }

    [Fact]
    public async Task Generated_solution_has_no_license_file()
    {
        var solution = await package.GenerateAsync("Contoso.Shop");

        Assert.DoesNotContain("LICENSE", solution.RelativeFiles());
    }
}
