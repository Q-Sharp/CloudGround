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

    [Theory]
    [InlineData(".github/dependabot.yml", "")]
    [InlineData("templates/cloudground/.github/dependabot.yml", "templates/cloudground")]
    public void Nuget_directories_contain_central_package_versions(string configuration, string root)
    {
        var repository = RepositoryRoot.Find();
        var lines = File.ReadAllLines(Path.Combine(repository, configuration));

        var directories = EcosystemDirectories(lines, "nuget");

        Assert.NotEmpty(directories);
        Assert.All(directories, directory =>
            Assert.True(File.Exists(Path.Combine(repository, root, directory.TrimStart('/'), "Directory.Packages.props")), $"{configuration}: {directory}"));
    }

    [Theory]
    [InlineData(".github/dependabot.yml")]
    [InlineData("templates/cloudground/.github/dependabot.yml")]
    public void Dependabot_ignores_major_updates_of_lts_bound_packages(string configuration)
    {
        var lines = File.ReadAllLines(Path.Combine(RepositoryRoot.Find(), configuration));

        var ignored = EcosystemSection(lines, "nuget")
            .Select(line => IgnoredDependency().Match(line))
            .Where(match => match.Success)
            .Select(match => match.Groups["dependency"].Value);

        Assert.Equal(LtsBoundPackages.Order(), ignored.Order());
    }

    private static readonly string[] LtsBoundPackages =
        ["Microsoft.Extensions.*", "Microsoft.AspNetCore.*", "Microsoft.EntityFrameworkCore.*", "dotnet-ef"];

    private static List<string> GithubActionsDirectories(string[] lines) => EcosystemDirectories(lines, "github-actions");

    private static List<string> EcosystemDirectories(string[] lines, string ecosystem) =>
        EcosystemSection(lines, ecosystem)
            .Select(line => DirectoryEntry().Match(line))
            .Where(match => match.Success)
            .Select(match => match.Groups["directory"].Value)
            .ToList();

    private static IEnumerable<string> EcosystemSection(string[] lines, string ecosystem) =>
        lines
            .SkipWhile(line => !line.Contains($"package-ecosystem: {ecosystem}", StringComparison.Ordinal))
            .Skip(1)
            .TakeWhile(line => !EcosystemStart().IsMatch(line));

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

    [GeneratedRegex(@"^\s+- dependency-name: (?<dependency>\S+)\s*$")]
    private static partial Regex IgnoredDependency();
}
