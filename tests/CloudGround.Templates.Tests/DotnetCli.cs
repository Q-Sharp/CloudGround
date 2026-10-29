namespace CloudGround.Templates.Tests;

public static class DotnetCli
{
    public static Task<CliResult> RunAsync(string workingDirectory, params string[] arguments) =>
        ProcessRunner.RunAsync("dotnet", workingDirectory, arguments);

    public static Task<CliResult> RunWithEnvironmentAsync(
        string workingDirectory,
        IReadOnlyDictionary<string, string> environment,
        params string[] arguments) =>
        ProcessRunner.RunAsync("dotnet", workingDirectory, environment, arguments);

    public static string[] TestArguments(string solutionFile) =>
        ["test", "--solution", solutionFile, "--filter-not-trait", "Category=EndToEnd", "--ignore-exit-code", "8"];
}
