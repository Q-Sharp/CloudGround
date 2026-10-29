using System.Text.Json;

namespace CloudGround.Templates.Tests;

[Collection(SharedTemplatePackage.Name)]
public sealed class AiKitTests(TemplatePackage package)
{
    private static readonly TimeSpan McpServerStartLimit = TimeSpan.FromMinutes(3);

    [Fact]
    public async Task Claude_instructions_import_the_agent_rules()
    {
        var solution = await package.GenerateAsync("Contoso.Shop");

        Assert.Equal("@AGENTS.md", File.ReadLines(Path.Combine(solution.Root, "CLAUDE.md")).First());
    }

    [Fact]
    public async Task Skills_are_named_after_their_folder()
    {
        var solution = await package.GenerateAsync("Contoso.Shop", "--sample", "false");

        SkillFrontMatter.AssertSkillsAreNamedAfterTheirFolder(solution.Root);
    }

    [Fact]
    public async Task Angular_mcp_server_starts_from_the_configured_command()
    {
        var solution = await package.GenerateAsync("Contoso.Shop");
        using var configuration = JsonDocument.Parse(File.ReadAllText(Path.Combine(solution.Root, ".mcp.json")));
        var server = configuration.RootElement.GetProperty("mcpServers").GetProperty("angular-cli");
        var arguments = server.GetProperty("args").EnumerateArray().Select(argument => argument.GetString()!).ToList();
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        limit.CancelAfter(McpServerStartLimit);

        var tools = await StdioMcpClient.ListToolsAsync(server.GetProperty("command").GetString()!, arguments, solution.Root, limit.Token);

        Assert.Contains("get_best_practices", tools);
        Assert.Contains("search_documentation", tools);
        Assert.Contains("list_projects", tools);
    }
}
