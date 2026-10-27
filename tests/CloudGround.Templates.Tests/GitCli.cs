namespace CloudGround.Templates.Tests;

public static class GitCli
{
    private static readonly string NoHooks = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "cloudground-no-git-hooks")).FullName;

    private static readonly string[] IsolatedConfiguration =
    [
        "-c", "user.name=test",
        "-c", "user.email=test@example.invalid",
        "-c", "commit.gpgsign=false",
        "-c", "tag.gpgsign=false",
        "-c", $"core.hooksPath={NoHooks}",
        "-c", "init.defaultBranch=main",
    ];

    public static Task<CliResult> RunAsync(string workingDirectory, params string[] arguments) =>
        ProcessRunner.RunAsync("git", workingDirectory, [.. IsolatedConfiguration, .. arguments]);
}
