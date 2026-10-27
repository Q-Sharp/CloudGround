namespace CloudGround.Templates.Tests;

public static class DotnetCli
{
    public static Task<CliResult> RunAsync(string workingDirectory, params string[] arguments) =>
        ProcessRunner.RunAsync("dotnet", workingDirectory, arguments);
}
