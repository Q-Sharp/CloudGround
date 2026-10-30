namespace CloudGround.Templates.Tests;

public sealed record CliResult(int ExitCode, string Output);

public static class ProcessRunner
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(10);

    private static readonly string[] InheritedBuildVariablePrefixes =
        ["MSBuild", "MSBUILD", "_MSBUILD", "VSTEST", "DOTNET_HOST_PATH", "DOTNET_CLI_TEST_", "TESTINGPLATFORM_"];

    private static readonly Dictionary<string, string> IsolatedBuildVariables = new()
    {
        ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1",
        ["DOTNET_NOLOGO"] = "1",
        ["MSBUILDDISABLENODEREUSE"] = "1",
        ["DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER"] = "1",
        ["UseSharedCompilation"] = "false",
        ["UseRazorBuildServer"] = "false",
    };

    public static Task<CliResult> RunAsync(string fileName, string workingDirectory, params string[] arguments) =>
        RunAsync(fileName, workingDirectory, new Dictionary<string, string>(), arguments);

    public static async Task<CliResult> RunAsync(
        string fileName,
        string workingDirectory,
        IReadOnlyDictionary<string, string> environmentOverrides,
        params string[] arguments)
    {
        var output = new StringBuilder();
        using var process = Start(fileName, workingDirectory, environmentOverrides, arguments, output, _ => { });

        using var timeout = new CancellationTokenSource(Timeout);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            Assert.Fail($"`{fileName} {string.Join(' ', arguments)}` timed out after {Timeout}.{Environment.NewLine}{output}");
        }

        return new CliResult(process.ExitCode, output.ToString());
    }

    public static async Task<CliResult> RunUntilAsync(
        string fileName,
        string workingDirectory,
        IReadOnlyDictionary<string, string> environmentOverrides,
        Func<string, bool> stopWhen,
        TimeSpan limit,
        params string[] arguments)
    {
        var output = new StringBuilder();
        var matched = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var process = Start(fileName, workingDirectory, environmentOverrides, arguments, output, line =>
        {
            if (stopWhen(line))
            {
                matched.TrySetResult();
            }
        });

        using var limitReached = new CancellationTokenSource(limit);
        await Task.WhenAny(process.WaitForExitAsync(limitReached.Token), matched.Task);
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }

        return new CliResult(process.ExitCode, output.ToString());
    }

    private static Process Start(
        string fileName,
        string workingDirectory,
        IReadOnlyDictionary<string, string> environmentOverrides,
        string[] arguments,
        StringBuilder output,
        Action<string> onLine)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        IsolateFromParentBuild(startInfo.Environment);
        foreach (var (name, value) in environmentOverrides)
        {
            startInfo.Environment[name] = value;
        }

        var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (_, line) => Receive(output, line.Data, onLine);
        process.ErrorDataReceived += (_, line) => Receive(output, line.Data, onLine);
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    private static void IsolateFromParentBuild(IDictionary<string, string?> environment)
    {
        foreach (var name in environment.Keys.Where(IsInheritedBuildVariable).ToList())
        {
            environment.Remove(name);
        }

        foreach (var (name, value) in IsolatedBuildVariables)
        {
            environment[name] = value;
        }
    }

    private static bool IsInheritedBuildVariable(string name) =>
        InheritedBuildVariablePrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal));

    private static void Receive(StringBuilder output, string? line, Action<string> onLine)
    {
        if (line is null)
        {
            return;
        }

        lock (output)
        {
            output.AppendLine(line);
        }

        onLine(line);
    }
}
