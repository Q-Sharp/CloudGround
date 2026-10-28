using System.Text.RegularExpressions;

namespace CloudGround.Templates.Tests;

[Collection(SharedTemplatePackage.Name)]
public sealed partial class PortTests(TemplatePackage package)
{
    private static readonly string[] ProjectsWithLaunchSettings = ["AppHost", "Api", "Bff"];

    [Fact]
    public async Task Two_generated_solutions_use_different_ports()
    {
        var first = await package.GenerateAsync("First.App");
        var second = await package.GenerateAsync("Second.App");

        Assert.NotEqual(LaunchPorts(first), LaunchPorts(second));
    }

    private static List<string> LaunchPorts(GeneratedSolution solution) =>
        ProjectsWithLaunchSettings
            .Select(project => Path.Combine(solution.Root, "src", $"{solution.Name}.{project}", "Properties", "launchSettings.json"))
            .SelectMany(file => LocalhostPort().Matches(File.ReadAllText(file)))
            .Select(match => match.Groups["port"].Value)
            .ToList();

    [GeneratedRegex(@"localhost:(?<port>\d+)")]
    private static partial Regex LocalhostPort();
}
