using System.Text.Json;

namespace CloudGround.Templates.Tests;

[Collection(SharedTemplatePackage.Name)]
public sealed class NamingTests(TemplatePackage package)
{
    private static readonly string[] SourceNameForms = ["cloudground.app", "cloudground_app", "cloudground-app", "cloudgroundapp"];

    private static readonly string[] ApiCookieNames = ["contoso-shop-session", "contoso-shop-antiforgery", "contoso-shop-xsrf", "contoso-shop-correlation.", "contoso-shop-nonce."];

    [Fact]
    public async Task Generated_solution_contains_no_source_name_form()
    {
        var solution = await package.GenerateAsync("Contoso.Shop");
        var files = solution.RelativeFiles();

        Assert.DoesNotContain(files, ContainsSourceNameForm);
        Assert.All(files, file => Assert.False(ContainsSourceNameForm(File.ReadAllText(Path.Combine(solution.Root, file))), file));
    }

    [Fact]
    public async Task Identity_names_follow_the_solution_name()
    {
        var solution = await package.GenerateAsync("Contoso.Shop");

        using var realm = JsonDocument.Parse(File.ReadAllText(Path.Combine(solution.Root, "src", "Contoso.Shop.AppHost", "Realms", "realm.json")));
        var clients = realm.RootElement.GetProperty("clients").EnumerateArray().Select(client => client.GetProperty("clientId").GetString());

        Assert.Equal("contoso-shop", realm.RootElement.GetProperty("realm").GetString());
        Assert.Equal(["contoso-shop-api", "contoso-shop-tests"], clients.Order());
    }

    [Fact]
    public async Task Cookie_names_follow_the_solution_name()
    {
        var solution = await package.GenerateAsync("Contoso.Shop");
        var api = File.ReadAllText(Path.Combine(solution.Root, "src", "Contoso.Shop.Api", "Authentication.cs"))
            + File.ReadAllText(Path.Combine(solution.Root, "src", "Contoso.Shop.Api", "XsrfProtection.cs"));
        var web = File.ReadAllText(Path.Combine(solution.Root, "src", "Contoso.Shop.Web", "src", "app", "app.config.ts"));

        Assert.All(ApiCookieNames, name => Assert.Contains($"\"{name}\"", api, StringComparison.Ordinal));
        Assert.Contains("cookieName: 'contoso-shop-xsrf'", web, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Generated_solution_builds_and_its_tests_pass()
    {
        var solution = await package.GenerateAsync("Contoso.Shop");

        var build = await DotnetCli.RunAsync(solution.Root, "build");
        Assert.True(build.ExitCode == 0, build.Output);

        var test = await DotnetCli.RunAsync(solution.Root, DotnetCli.TestArguments("Contoso.Shop.slnx"));
        Assert.True(test.ExitCode == 0, test.Output);
    }

    [Fact]
    public async Task Hyphenated_lowercase_name_builds()
    {
        var solution = await package.GenerateAsync("my-shop");

        Assert.Contains("src/my_shop.Api/Program.cs", solution.RelativeFiles());

        var build = await DotnetCli.RunAsync(solution.Root, "build");
        Assert.True(build.ExitCode == 0, build.Output);
    }

    [Fact]
    public async Task Solution_named_after_microsoft_passes_the_format_check()
    {
        var solution = await package.GenerateAsync("Tailspin.Shop");

        var format = await DotnetCli.RunAsync(solution.Root, "format", "--verify-no-changes");

        Assert.True(format.ExitCode == 0, format.Output);
    }

    private static bool ContainsSourceNameForm(string text) =>
        SourceNameForms.Any(form => text.Contains(form, StringComparison.OrdinalIgnoreCase));
}
