using System.Diagnostics;
using System.Text;

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

    public static async Task<CliResult> RunAsync(string fileName, string workingDirectory, params string[] arguments)
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

        using var process = new Process { StartInfo = startInfo };
        var output = new StringBuilder();
        process.OutputDataReceived += (_, line) => Append(output, line.Data);
        process.ErrorDataReceived += (_, line) => Append(output, line.Data);
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

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

    private static void Append(StringBuilder output, string? line)
    {
        if (line is null)
        {
            return;
        }

        lock (output)
        {
            output.AppendLine(line);
        }
    }
}
