using System.Reflection;
using System.Text.RegularExpressions;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Docker;

namespace CloudGround.App.Integration.Tests;

[Collection(PublishedDeployment.Name)]
public sealed class ComposeFileTests(PublishedCompose compose)
{
    private const string KeycloakAddress = "Keycloak__Url: \"${KEYCLOAK_URL}\"";

    private static readonly string SolutionVersion = typeof(ComposeFileTests).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
        .InformationalVersion
        .Split('+')[0];

    [Fact]
    public void Identity_provider_is_configured_not_deployed()
    {
        Assert.DoesNotContain("keycloak", compose.ServiceNames);
        Assert.Contains(KeycloakAddress, compose.Service("api"), StringComparison.Ordinal);
        Assert.Contains("Keycloak__ClientSecret: \"${API_CLIENT_SECRET}\"", compose.Service("api"), StringComparison.Ordinal);
    }

    [Fact]
    public void Only_the_api_and_the_dashboard_publish_ports()
    {
        var publishing = compose.ServiceNames.Where(name => compose.Service(name).Contains("ports:", StringComparison.Ordinal));

        Assert.Equal(["api", "compose-dashboard"], publishing.Order());
    }

    [Theory]
    [InlineData("api", "${API_PORT}")]
    [InlineData("compose-dashboard", "18888")]
    public void Published_ports_listen_on_the_loopback_interface_only(string service, string port)
    {
        Assert.Contains($"- \"127.0.0.1:{port}:{port}\"", compose.Service(service), StringComparison.Ordinal);
    }

    [Fact]
    public void Api_listens_on_the_default_container_port()
    {
        Assert.Contains("HTTP_PORTS: \"${API_PORT}\"", compose.Service("api"), StringComparison.Ordinal);
        Assert.Contains("API_PORT=8080", compose.EnvironmentFile.Split('\n'));
    }

    [Fact]
    public void Api_trusts_the_forwarded_headers_of_the_reverse_proxy_in_front()
    {
        Assert.Contains("ASPNETCORE_FORWARDEDHEADERS_ENABLED: \"true\"", compose.Service("api"), StringComparison.Ordinal);
    }

    [Fact]
    public void Dashboard_runs_the_released_image_of_the_aspire_version()
    {
        var aspire = typeof(DockerComposeEnvironmentResource).Assembly.GetName().Version!;

        Assert.Contains($"image: \"mcr.microsoft.com/dotnet/aspire-dashboard:{aspire.Major}\"", compose.Service("compose-dashboard"), StringComparison.Ordinal);
    }

    [Fact]
    public void Compose_project_is_named_after_the_solution()
    {
        Assert.Equal("cloudground-app", compose.ProjectName);
    }

    [Fact]
    public void Web_runs_its_image_entrypoint_and_accepts_the_api()
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
    public void Long_running_services_restart_unless_stopped(string service)
    {
        Assert.Contains("restart: \"unless-stopped\"", compose.Service(service), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Every_built_image_is_tagged_with_the_solution_version()
    {
        var cancellation = TestContext.Current.CancellationToken;
        await using var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.CloudGround_App_AppHost>(
            ["--operation", "publish", "--step", "publish", "--output-path", compose.OutputDirectory],
            cancellation);

        var images = appHost.Resources.OfType<IComputeResource>().Where(resource => resource.RequiresImageBuild()).ToList();

        Assert.NotEmpty(images);
        foreach (var image in images)
        {
            Assert.Equal(SolutionVersion, await RemoteImageTagAsync(image, cancellation));
        }
    }

    [Theory]
    [InlineData("postgres", "pg_isready")]
    [InlineData("messaging", "rabbitmq-diagnostics")]
    public void Database_and_broker_report_their_health(string service, string probe)
    {
        Assert.Matches(new Regex($@"healthcheck:\s+test:\s+- ""CMD""\s+- ""{probe}"""), compose.Service(service));
    }

    [Theory]
    [InlineData("postgres")]
    [InlineData("messaging")]
    public void Api_waits_until_database_and_broker_are_healthy(string dependency)
    {
        Assert.Matches(new Regex($@"{dependency}:\s+condition: ""service_healthy"""), compose.Service("api"));
    }

    [Theory]
    [InlineData("postgres", "postgres-data")]
    [InlineData("cache", "cache-data")]
    public void Data_volumes_keep_their_names_across_machines(string service, string volume)
    {
        Assert.Contains($"source: \"{volume}\"", compose.Service(service), StringComparison.Ordinal);
    }

#pragma warning disable ASPIREPIPELINES003
    private static async Task<string?> RemoteImageTagAsync(IResource image, CancellationToken cancellation)
    {
        var context = new ContainerImagePushOptionsCallbackContext
        {
            Resource = image,
            Options = new ContainerImagePushOptions(),
            CancellationToken = cancellation,
        };
        foreach (var annotation in image.Annotations.OfType<ContainerImagePushOptionsCallbackAnnotation>())
        {
            await annotation.Callback(context);
        }

        return context.Options.RemoteImageTag;
    }
#pragma warning restore ASPIREPIPELINES003
}
