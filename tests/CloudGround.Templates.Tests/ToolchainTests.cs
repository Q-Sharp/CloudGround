namespace CloudGround.Templates.Tests;

[Collection(SharedTemplatePackage.Name)]
public sealed class ToolchainTests(TemplatePackage package)
{
    private static readonly string[] AspireLaunchers = ["aspire", "dnx"];

    private static readonly string[] Prerequisites = ["dotnet", "node", "npm", "npx"];

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

        var test = await RunOnFreshMachineAsync(solution, DotnetCli.TestArguments("Untrusted.App.slnx"));

        Assert.True(test.ExitCode == 0, test.Output);
    }

    [Fact]
    public async Task Generated_solution_restores_its_tools_on_a_fresh_machine()
    {
        var solution = await package.GenerateAsync("Tools.App");

        var restore = await RunOnFreshMachineAsync(solution, "tool", "restore");

        Assert.True(restore.ExitCode == 0, restore.Output);
    }

    private static async Task<CliResult> RunOnFreshMachineAsync(GeneratedSolution solution, params string[] arguments)
    {
        var machine = Directory.CreateTempSubdirectory("cloudground-machine-").FullName;
        try
        {
            var prerequisites = Directory.CreateDirectory(Path.Combine(machine, "bin")).FullName;
            foreach (var executable in Prerequisites)
            {
                File.CreateSymbolicLink(Path.Combine(prerequisites, executable), FindOnPath(executable));
            }

            var freshMachine = new Dictionary<string, string>
            {
                ["PATH"] = string.Join(Path.PathSeparator, [prerequisites, .. PathEntries().Where(HasNoAspireLauncher)]),
                ["HOME"] = Directory.CreateDirectory(Path.Combine(machine, "home")).FullName,
                ["NUGET_PACKAGES"] = NuGetPackagesFolder(),
                ["XDG_DATA_HOME"] = ContainerRuntimeFolder("XDG_DATA_HOME", ".local", "share"),
                ["XDG_CONFIG_HOME"] = ContainerRuntimeFolder("XDG_CONFIG_HOME", ".config"),
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

    private static string ContainerRuntimeFolder(string variable, params string[] defaultBelowHome) =>
        Environment.GetEnvironmentVariable(variable)
        ?? Path.Combine([Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), .. defaultBelowHome]);

    private static bool HasNoAspireLauncher(string directory) =>
        !AspireLaunchers.Any(launcher => File.Exists(Path.Combine(directory, launcher)));
}
