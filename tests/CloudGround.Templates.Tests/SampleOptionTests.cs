namespace CloudGround.Templates.Tests;

[Collection(SharedTemplatePackage.Name)]
public sealed class SampleOptionTests(TemplatePackage package)
{
    private static readonly string[] SampleReferences = ["ProjectsModule", "Modules.Projects", "projects-migrations", "TasksModule", "Modules.Tasks", "tasks-migrations"];

    private static readonly string[] TemplateDirectiveStarts = ["#if", "#elif", "#else", "#endif", "<!--#"];

    [Fact]
    public async Task Solution_without_sample_has_no_modules()
    {
        var solution = await package.GenerateAsync("Contoso.Empty", "--sample", "false");
        var files = solution.RelativeFiles();

        Assert.DoesNotContain(files, file => file.Contains("Modules", StringComparison.Ordinal));
        Assert.DoesNotContain(files, file => Path.GetFileName(file).StartsWith("Projects", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("Modules", File.ReadAllText(Path.Combine(solution.Root, "Contoso.Empty.slnx")), StringComparison.Ordinal);
        Assert.All(files, file => Assert.False(ReferencesTheSample(ReadFile(solution, file)), file));
    }

    [Fact]
    public async Task Solution_without_sample_builds_and_its_tests_pass()
    {
        var solution = await package.GenerateAsync("Contoso.Empty", "--sample", "false");

        var build = await DotnetCli.RunAsync(solution.Root, "build");
        Assert.True(build.ExitCode == 0, build.Output);

        var test = await DotnetCli.RunAsync(solution.Root, DotnetCli.TestArguments("Contoso.Empty.slnx"));
        Assert.True(test.ExitCode == 0, test.Output);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    public async Task Generated_solutions_contain_no_template_directives(string sample)
    {
        var solution = await package.GenerateAsync("Contoso.Clean", "--sample", sample);

        Assert.All(solution.RelativeFiles(), file =>
            Assert.DoesNotContain(ReadFile(solution, file).Split('\n'), IsTemplateDirective));
        Assert.DoesNotContain("sample", ReadFile(solution, "Directory.Build.props"), StringComparison.OrdinalIgnoreCase);
    }

    private static bool ReferencesTheSample(string text) =>
        SampleReferences.Any(reference => text.Contains(reference, StringComparison.Ordinal));

    private static bool IsTemplateDirective(string line) =>
        TemplateDirectiveStarts.Any(start => line.TrimStart().StartsWith(start, StringComparison.Ordinal));

    private static string ReadFile(GeneratedSolution solution, string relativePath) =>
        File.ReadAllText(Path.Combine(solution.Root, relativePath));
}
