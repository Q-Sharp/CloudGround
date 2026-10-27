namespace CloudGround.Templates.Tests;

[Collection(SharedTemplatePackage.Name)]
public sealed class ToolchainTests(TemplatePackage package)
{
    private static readonly string[] AspireLaunchers = ["aspire", "dnx"];

    [Fact]
    public async Task Generated_solution_builds_without_aspire_cli_or_dnx_on_path()
    {
        var solution = await package.GenerateAsync("Offline.App");
        var machine = Directory.CreateTempSubdirectory("cloudground-machine-").FullName;
        try
        {
            var dotnetOnly = Directory.CreateDirectory(Path.Combine(machine, "bin")).FullName;
            File.CreateSymbolicLink(Path.Combine(dotnetOnly, "dotnet"), FindOnPath("dotnet"));
            var freshMachine = new Dictionary<string, string>
            {
                ["PATH"] = string.Join(Path.PathSeparator, [dotnetOnly, .. PathEntries().Where(HasNoAspireLauncher)]),
                ["HOME"] = Directory.CreateDirectory(Path.Combine(machine, "home")).FullName,
                ["NUGET_PACKAGES"] = NuGetPackagesFolder(),
            };

            var build = await DotnetCli.RunWithEnvironmentAsync(solution.Root, freshMachine, "build");

            Assert.True(build.ExitCode == 0, build.Output);
        }
        finally
        {
            Directory.Delete(machine, recursive: true);
        }
    }

    private static string[] PathEntries() =>
        (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

    private static string FindOnPath(string executable) =>
        PathEntries().Select(entry => Path.Combine(entry, executable)).First(File.Exists);

    private static string NuGetPackagesFolder() =>
        Environment.GetEnvironmentVariable("NUGET_PACKAGES")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");

    private static bool HasNoAspireLauncher(string directory) =>
        !AspireLaunchers.Any(launcher => File.Exists(Path.Combine(directory, launcher)));
}
