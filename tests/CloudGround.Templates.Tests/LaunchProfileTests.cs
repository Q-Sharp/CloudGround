namespace CloudGround.Templates.Tests;

[Collection(SharedTemplatePackage.Name)]
public sealed class LaunchProfileTests(TemplatePackage package)
{
    [Fact]
    public async Task AppHost_http_profile_allows_unsecured_transport()
    {
        var solution = await package.GenerateAsync("Contoso.Shop");
        var launchSettings = Path.Combine(solution.Root, "src", "Contoso.Shop.AppHost", "Properties", "launchSettings.json");

        using var document = JsonDocument.Parse(File.ReadAllText(launchSettings));
        var environment = document.RootElement.GetProperty("profiles").GetProperty("http").GetProperty("environmentVariables");

        Assert.True(environment.TryGetProperty("ASPIRE_ALLOW_UNSECURED_TRANSPORT", out var allowUnsecured));
        Assert.Equal("true", allowUnsecured.GetString());
    }
}
