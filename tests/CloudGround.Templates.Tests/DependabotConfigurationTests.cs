using System.Text.RegularExpressions;

namespace CloudGround.Templates.Tests;

public sealed partial class DependabotConfigurationTests
{
    [Theory]
    [InlineData(".github/dependabot.yml", "")]
    [InlineData("templates/cloudground/.github/dependabot.yml", "templates/cloudground")]
    public void Github_actions_directories_contain_what_dependabot_scans(string configuration, string root)
    {
        var repository = RepositoryRoot.Find();
        var lines = File.ReadAllLines(Path.Combine(repository, configuration));

        var directories = GithubActionsDirectories(lines);

        Assert.NotEmpty(directories);
        Assert.All(directories, directory =>
            Assert.True(ContainsScannedYaml(Path.Combine(repository, root), directory), $"{configuration}: {directory}"));
    }

    private static List<string> GithubActionsDirectories(string[] lines) =>
        lines
            .SkipWhile(line => !line.Contains("package-ecosystem: github-actions", StringComparison.Ordinal))
            .Skip(1)
            .TakeWhile(line => !EcosystemStart().IsMatch(line))
            .Select(line => DirectoryEntry().Match(line))
            .Where(match => match.Success)
            .Select(match => match.Groups["directory"].Value)
            .ToList();

    private static bool ContainsScannedYaml(string root, string directory)
    {
        var scanned = directory == "/"
            ? Path.Combine(root, ".github", "workflows")
            : Path.Combine(root, directory.TrimStart('/'));

        return Directory.Exists(scanned)
            && Directory.EnumerateFiles(scanned).Any(file => file.EndsWith(".yml", StringComparison.Ordinal) || file.EndsWith(".yaml", StringComparison.Ordinal));
    }

    [GeneratedRegex(@"^\s*- package-ecosystem:")]
    private static partial Regex EcosystemStart();

    [GeneratedRegex(@"^\s+- (?<directory>/\S*)\s*$")]
    private static partial Regex DirectoryEntry();
}
