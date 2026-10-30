namespace CloudGround.Templates.Tests;

[Collection(SharedTemplatePackage.Name)]
public sealed class VersioningTests(TemplatePackage package)
{
    [Fact]
    public async Task Generated_solution_builds_without_git_and_gets_prerelease_version()
    {
        var solution = await package.GenerateAsync("Untagged.App");

        await BuildAsync(solution);

        Assert.StartsWith("0.0.0-alpha.0", ApiProductVersion(solution), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Generated_solution_takes_version_from_tag()
    {
        var solution = await package.GenerateAsync("Tagged.App");
        await GitAsync(solution, "init");
        await GitAsync(solution, "add", "-A");
        await GitAsync(solution, "commit", "-m", "init");
        await GitAsync(solution, "tag", "v1.2.3");

        await BuildAsync(solution);

        Assert.StartsWith("1.2.3", ApiProductVersion(solution), StringComparison.Ordinal);
    }

    private static async Task BuildAsync(GeneratedSolution solution)
    {
        var build = await DotnetCli.RunAsync(solution.Root, "build");
        Assert.True(build.ExitCode == 0, build.Output);
    }

    private static async Task GitAsync(GeneratedSolution solution, params string[] arguments)
    {
        var git = await GitCli.RunAsync(solution.Root, arguments);
        Assert.True(git.ExitCode == 0, git.Output);
    }

    private static string ApiProductVersion(GeneratedSolution solution)
    {
        var assembly = Path.Combine(solution.Root, "src", $"{solution.Name}.Api", "bin", "Debug", "net10.0", $"{solution.Name}.Api.dll");
        return FileVersionInfo.GetVersionInfo(assembly).ProductVersion ?? string.Empty;
    }
}
