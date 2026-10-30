namespace CloudGround.Templates.Tests;

public static class StdioMcpClient
{
    private const string ProtocolVersion = "2025-06-18";

    public static async Task<IReadOnlyList<string>> ListToolsAsync(
        string command,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(command)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var server = Process.Start(startInfo)!;
        server.ErrorDataReceived += (_, _) => { };
        server.BeginErrorReadLine();
        try
        {
            await SendAsync(server, new JsonObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = 1,
                ["method"] = "initialize",
                ["params"] = new JsonObject
                {
                    ["protocolVersion"] = ProtocolVersion,
                    ["capabilities"] = new JsonObject(),
                    ["clientInfo"] = new JsonObject { ["name"] = "cloudground-tests", ["version"] = "1.0.0" },
                },
            }, cancellationToken);
            await ReceiveAsync(server, 1, cancellationToken);

            await SendAsync(server, new JsonObject { ["jsonrpc"] = "2.0", ["method"] = "notifications/initialized" }, cancellationToken);
            await SendAsync(server, new JsonObject { ["jsonrpc"] = "2.0", ["id"] = 2, ["method"] = "tools/list" }, cancellationToken);
            var tools = await ReceiveAsync(server, 2, cancellationToken);

            return tools.GetProperty("result").GetProperty("tools").EnumerateArray()
                .Select(tool => tool.GetProperty("name").GetString()!)
                .ToList();
        }
        finally
        {
            server.Kill(entireProcessTree: true);
            await server.WaitForExitAsync(CancellationToken.None);
        }
    }

    private static async Task SendAsync(Process server, JsonObject message, CancellationToken cancellationToken)
    {
        await server.StandardInput.WriteLineAsync(message.ToJsonString().AsMemory(), cancellationToken);
        await server.StandardInput.FlushAsync(cancellationToken);
    }

    private static async Task<JsonElement> ReceiveAsync(Process server, int id, CancellationToken cancellationToken)
    {
        while (await server.StandardOutput.ReadLineAsync(cancellationToken) is { } line)
        {
            if (TryParse(line) is { } message
                && message.TryGetProperty("id", out var messageId)
                && messageId.ValueKind == JsonValueKind.Number
                && messageId.GetInt32() == id)
            {
                return message;
            }
        }

        throw new InvalidOperationException($"The MCP server closed its output before answering request {id}.");
    }

    private static JsonElement? TryParse(string line)
    {
        try
        {
            return JsonDocument.Parse(line).RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
