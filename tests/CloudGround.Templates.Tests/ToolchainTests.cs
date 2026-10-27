namespace CloudGround.Templates.Tests;

[Collection(SharedTemplatePackage.Name)]
public sealed class ToolchainTests(TemplatePackage package)
{
    private static readonly string[] AspireLaunchers = ["aspire", "dnx"];

    [Fact]
    public async Task Generated_solution_builds_without_aspire_cli_or_dnx_on_path()
    {
        var solution = await package.GenerateAsync("Offline.App");

        var build = await RunOnFreshMachineAsync(solution, "build");

        Assert.True(build.ExitCode == 0, build.Output);
    }

    [Fact]
    public async Task Generated_solution_tests_pass_without_a_trusted_developer_certificate()
    {
        var solution = await package.GenerateAsync("Untrusted.App");

        var test = await RunOnFreshMachineAsync(solution, "test", "--solution", "Untrusted.App.slnx");

        Assert.True(test.ExitCode == 0, test.Output);
    }

    private static async Task<CliResult> RunOnFreshMachineAsync(GeneratedSolution solution, params string[] arguments)
    {
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

            return await DotnetCli.RunWithEnvironmentAsync(solution.Root, freshMachine, arguments);
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
