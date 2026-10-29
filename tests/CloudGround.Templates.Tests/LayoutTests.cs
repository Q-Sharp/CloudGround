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
    [InlineData("src/Contoso.Shop.Bff/Contoso.Shop.Bff.csproj")]
    [InlineData("src/Contoso.Shop.Web/package.json")]
    [InlineData("src/Contoso.Shop.Web/package-lock.json")]
    [InlineData("src/Contoso.Shop.Web/angular.json")]
    [InlineData("src/Contoso.Shop.Web/.nvmrc")]
    [InlineData("src/Contoso.Shop.Web/Dockerfile")]
    [InlineData("src/Contoso.Shop.Web/.dockerignore")]
    [InlineData("src/Contoso.Shop.AppHost/Realms/realm.json")]
    [InlineData("src/Modules/Contoso.Shop.Modules.Projects/Contoso.Shop.Modules.Projects.csproj")]
    [InlineData("src/Modules/Contoso.Shop.Modules.Projects.Contracts/Contoso.Shop.Modules.Projects.Contracts.csproj")]
    [InlineData("src/Modules/Contoso.Shop.Modules.Tasks/Contoso.Shop.Modules.Tasks.csproj")]
    [InlineData("tests/Contoso.Shop.Integration.Tests/Contoso.Shop.Integration.Tests.csproj")]
    [InlineData("tests/Contoso.Shop.Architecture.Tests/Contoso.Shop.Architecture.Tests.csproj")]
    [InlineData("tests/Contoso.Shop.Bff.Tests/Contoso.Shop.Bff.Tests.csproj")]
    [InlineData("tests/Contoso.Shop.EndToEnd.Tests/Contoso.Shop.EndToEnd.Tests.csproj")]
    [InlineData("tests/Contoso.Shop.Modules.Projects.Tests/Contoso.Shop.Modules.Projects.Tests.csproj")]
    [InlineData("tests/Contoso.Shop.Modules.Tasks.Tests/Contoso.Shop.Modules.Tasks.Tests.csproj")]
    [InlineData(".config/dotnet-tools.json")]
    [InlineData(".devcontainer/devcontainer.json")]
    [InlineData(".github/actions/build-and-test/action.yml")]
    [InlineData(".github/workflows/ci.yml")]
    [InlineData(".github/workflows/pr-title.yml")]
    [InlineData(".github/workflows/release-please.yml")]
    [InlineData("release-please-config.json")]
    [InlineData(".release-please-manifest.json")]
    [InlineData(".github/dependabot.yml")]
    [InlineData(".github/pull_request_template.md")]
    [InlineData("README.md")]
    [InlineData("AGENTS.md")]
    [InlineData("CLAUDE.md")]
    [InlineData(".mcp.json")]
    [InlineData(".claude/skills/add-module/SKILL.md")]
    [InlineData(".claude/skills/add-feature/SKILL.md")]
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

    [Fact]
    public async Task Generated_gitignore_ignores_artifacts_output()
    {
        var solution = await package.GenerateAsync("Contoso.Shop");

        Assert.Contains("artifacts/", File.ReadAllLines(Path.Combine(solution.Root, ".gitignore")));
    }
}
