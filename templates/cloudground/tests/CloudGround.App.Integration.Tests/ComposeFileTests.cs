namespace CloudGround.App.Integration.Tests;

[Collection(PublishedDeployment.Name)]
public sealed class ComposeFileTests(PublishedCompose compose)
{
    private const string KeycloakAddress = "services__keycloak__default__0: \"${KEYCLOAK_URL}\"";

    [Fact]
    public void Identity_provider_is_configured_not_deployed()
    {
        Assert.DoesNotContain("keycloak", compose.ServiceNames);
        Assert.Contains(KeycloakAddress, compose.Service("api"), StringComparison.Ordinal);
        Assert.Contains(KeycloakAddress, compose.Service("bff"), StringComparison.Ordinal);
    }

    [Fact]
    public void Only_the_bff_and_the_dashboard_publish_ports()
    {
        var publishing = compose.ServiceNames.Where(name => compose.Service(name).Contains("ports:", StringComparison.Ordinal));

        Assert.Equal(["bff", "compose-dashboard"], publishing.Order());
    }

    [Fact]
    public void Web_runs_its_image_entrypoint_and_accepts_the_bff()
    {
        var web = compose.Service("web");

        Assert.DoesNotContain("command:", web, StringComparison.Ordinal);
        Assert.DoesNotContain("NODE_OPTIONS", web, StringComparison.Ordinal);
        Assert.Contains("NG_ALLOWED_HOSTS: \"web\"", web, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("postgres")]
    [InlineData("cache")]
    [InlineData("messaging")]
    [InlineData("api")]
    [InlineData("web")]
    [InlineData("bff")]
    public void Long_running_services_restart_unless_stopped(string service)
    {
        Assert.Contains("restart: \"unless-stopped\"", compose.Service(service), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("postgres", "postgres-data")]
    [InlineData("cache", "cache-data")]
    public void Data_volumes_keep_their_names_across_machines(string service, string volume)
    {
        Assert.Contains($"source: \"{volume}\"", compose.Service(service), StringComparison.Ordinal);
    }
}
