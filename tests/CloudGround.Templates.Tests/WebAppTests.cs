namespace CloudGround.Templates.Tests;

[Collection(SharedTemplatePackage.Name)]
public sealed class WebAppTests(TemplatePackage package)
{
    private const string ServerImportMessage = "Server-only code belongs in src/server.ts or a *.server.ts file";

    private static readonly string[][] Steps = [["ci"], ["run", "build"], ["test"]];

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    public async Task Generated_web_app_lints_builds_and_tests(string sample)
    {
        var solution = await package.GenerateAsync("Contoso.Shop", "--sample", sample);
        var web = Path.Combine(solution.Root, "src", "Contoso.Shop.Web");

        foreach (var step in Steps)
        {
            var result = await ProcessRunner.RunAsync("npm", web, step);
            Assert.True(result.ExitCode == 0, $"npm {string.Join(' ', step)}: {result.Output}");
        }
    }

    [Fact]
    public async Task Generated_web_app_rejects_server_imports_in_browser_code()
    {
        var solution = await package.GenerateAsync("Lint.Shop", "--sample", "false");
        var web = Path.Combine(solution.Root, "src", "Lint.Shop.Web");
        var install = await ProcessRunner.RunAsync("npm", web, "ci");
        Assert.True(install.ExitCode == 0, install.Output);
        await File.WriteAllLinesAsync(
            Path.Combine(web, "src", "app", "leak.ts"),
            [
                "import { RenderMode } from '@angular/ssr';",
                "import '@angular/platform-server/init';",
                "import express from 'express';",
                "export const leaked = [RenderMode, express];",
            ],
            TestContext.Current.CancellationToken);

        var lint = await ProcessRunner.RunAsync("npm", web, "run", "lint");

        Assert.NotEqual(0, lint.ExitCode);
        Assert.Equal(3, lint.Output.Split(ServerImportMessage).Length - 1);
    }
}
