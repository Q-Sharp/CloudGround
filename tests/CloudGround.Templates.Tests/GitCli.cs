namespace CloudGround.Templates.Tests;

public static class GitCli
{
    private static readonly string[] IsolatedConfiguration =
    [
        "-c", "user.name=test",
        "-c", "user.email=test@example.invalid",
        "-c", "commit.gpgsign=false",
        "-c", "tag.gpgsign=false",
        "-c", "core.hooksPath=/dev/null",
        "-c", "init.defaultBranch=main",
    ];

    public static Task<CliResult> RunAsync(string workingDirectory, params string[] arguments) =>
        ProcessRunner.RunAsync("git", workingDirectory, [.. IsolatedConfiguration, .. arguments]);
}
