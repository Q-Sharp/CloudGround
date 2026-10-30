using System.Text.RegularExpressions;

namespace CloudGround.Templates.Tests;

public sealed partial class DependabotConfigurationTests
{
    private const string MajorUpdates = "version-update:semver-major";
    private const string MinorUpdates = "version-update:semver-minor";
    private const string PatchUpdates = "version-update:semver-patch";

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
    [InlineData(".github/dependabot.yml", "", "npm", "package-lock.json")]
    [InlineData("templates/cloudground/.github/dependabot.yml", "templates/cloudground", "npm", "package-lock.json")]
    [InlineData(".github/dependabot.yml", "", "docker", "Dockerfile")]
    [InlineData("templates/cloudground/.github/dependabot.yml", "templates/cloudground", "docker", "Dockerfile")]
    [InlineData(".github/dependabot.yml", "", "devcontainers", ".devcontainer/devcontainer.json")]
    [InlineData("templates/cloudground/.github/dependabot.yml", "templates/cloudground", "devcontainers", ".devcontainer/devcontainer.json")]
    public void Ecosystem_directories_contain_what_dependabot_reads(string configuration, string root, string ecosystem, string manifest)
    {
        var repository = RepositoryRoot.Find();
        var lines = File.ReadAllLines(Path.Combine(repository, configuration));

        var directories = EcosystemDirectories(lines, ecosystem);

        Assert.NotEmpty(directories);
        Assert.All(directories, directory =>
            Assert.True(File.Exists(Path.Combine(repository, root, directory.TrimStart('/'), manifest)), $"{configuration}: {directory}"));
    }

    [Theory]
    [InlineData(".github/dependabot.yml", "nuget", new[] { "Microsoft.Extensions.*", "Microsoft.AspNetCore.*", "Microsoft.EntityFrameworkCore.*", "dotnet-ef" })]
    [InlineData("templates/cloudground/.github/dependabot.yml", "nuget", new[] { "Microsoft.Extensions.*", "Microsoft.AspNetCore.*", "Microsoft.EntityFrameworkCore.*", "dotnet-ef" })]
    [InlineData(".github/dependabot.yml", "npm", new[] { "@types/node", "typescript" })]
    [InlineData("templates/cloudground/.github/dependabot.yml", "npm", new[] { "@types/node", "typescript" })]
    [InlineData(".github/dependabot.yml", "docker", new[] { "node" })]
    [InlineData("templates/cloudground/.github/dependabot.yml", "docker", new[] { "node" })]
    public void Dependabot_leaves_major_updates_to_deliberate_pull_requests(string configuration, string ecosystem, string[] deliberate)
    {
        var lines = File.ReadAllLines(Path.Combine(RepositoryRoot.Find(), configuration));

        var ignored = IgnoredUpdates(lines, ecosystem)
            .Where(update => update.UpdateTypes.Contains(MajorUpdates))
            .Select(update => update.Dependency);

        Assert.Equal(deliberate.Order(), ignored.Order());
    }

    [Theory]
    [InlineData(".github/dependabot.yml")]
    [InlineData("templates/cloudground/.github/dependabot.yml")]
    public void Dependabot_leaves_typescript_minor_and_major_updates_to_angular(string configuration)
    {
        var lines = File.ReadAllLines(Path.Combine(RepositoryRoot.Find(), configuration));

        var typescript = Assert.Single(IgnoredUpdates(lines, "npm"), update => update.Dependency == "typescript");
        Assert.Equal([MajorUpdates, MinorUpdates], typescript.UpdateTypes.Order());
    }

    [Theory]
    [InlineData(".github/dependabot.yml")]
    [InlineData("templates/cloudground/.github/dependabot.yml")]
    public void Angular_and_typescript_update_apart_from_the_other_npm_packages(string configuration)
    {
        var lines = File.ReadAllLines(Path.Combine(RepositoryRoot.Find(), configuration));

        var groups = EcosystemSection(lines, "npm")
            .Select(line => GroupPatterns().Match(line))
            .Where(match => match.Success)
            .Select(match => match.Groups["patterns"].Value)
            .ToList();

        Assert.Equal(["\"@angular/*\", \"angular-eslint\", \"typescript\"", "\"*\""], groups);
    }

    [Theory]
    [InlineData(".github/dependabot.yml")]
    [InlineData("templates/cloudground/.github/dependabot.yml")]
    public void Every_update_is_committed_as_a_fix(string configuration)
    {
        var lines = File.ReadAllLines(Path.Combine(RepositoryRoot.Find(), configuration));

        var ecosystems = lines
            .Select(line => EcosystemStart().Match(line))
            .Where(match => match.Success)
            .Select(match => match.Groups["ecosystem"].Value)
            .ToList();

        Assert.NotEmpty(ecosystems);
        Assert.All(ecosystems, ecosystem =>
        {
            var section = EcosystemSection(lines, ecosystem).ToList();
            Assert.Contains(section, line => FixPrefix().IsMatch(line));
            Assert.Contains(section, line => ScopeIncluded().IsMatch(line));
        });
    }

    private static List<(string Dependency, string[] UpdateTypes)> IgnoredUpdates(string[] lines, string ecosystem)
    {
        var section = EcosystemSection(lines, ecosystem).ToList();

        return section
            .Select((line, index) => (Dependency: IgnoredDependency().Match(line), Index: index))
            .Where(entry => entry.Dependency.Success)
            .Select(entry => (entry.Dependency.Groups["dependency"].Value, IgnoredUpdateTypes(section.ElementAtOrDefault(entry.Index + 1))))
            .ToList();
    }

    private static string[] IgnoredUpdateTypes(string? line)
    {
        var types = UpdateTypes().Match(line ?? string.Empty);

        return types.Success
            ? types.Groups["types"].Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            : [MajorUpdates, MinorUpdates, PatchUpdates];
    }

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

    [GeneratedRegex(@"^\s*- package-ecosystem: (?<ecosystem>\S+)")]
    private static partial Regex EcosystemStart();

    [GeneratedRegex(@"^\s+update-types: \[(?<types>[^\]]*)\]\s*$")]
    private static partial Regex UpdateTypes();

    [GeneratedRegex(@"^\s+prefix: fix\s*$")]
    private static partial Regex FixPrefix();

    [GeneratedRegex(@"^\s+include: scope\s*$")]
    private static partial Regex ScopeIncluded();

    [GeneratedRegex(@"^\s+patterns: \[(?<patterns>[^\]]*)\]\s*$")]
    private static partial Regex GroupPatterns();

    [GeneratedRegex(@"^\s+- (?<directory>/\S*)\s*$")]
    private static partial Regex DirectoryEntry();

    [GeneratedRegex(@"^\s+- dependency-name: ""?(?<dependency>[^""\s]+)""?\s*$")]
    private static partial Regex IgnoredDependency();
}
