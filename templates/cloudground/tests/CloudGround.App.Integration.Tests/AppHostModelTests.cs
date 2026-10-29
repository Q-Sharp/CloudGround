using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CloudGround.App.Integration.Tests;

public sealed class AppHostModelTests
{
    private const string ModulePrefix = "CloudGround.App.Modules.";

    [Fact]
    public async Task Database_server_keeps_its_data_in_a_volume()
    {
        await using var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.CloudGround_App_AppHost>(TestContext.Current.CancellationToken);

        var postgres = appHost.Resources.Single(resource => resource.Name == "postgres");

        Assert.Contains(postgres.Annotations.OfType<ContainerMountAnnotation>(), mount => mount.Type == ContainerMountType.Volume);
    }

    [Fact]
    public async Task Identity_provider_keeps_its_data_in_a_volume()
    {
        await using var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.CloudGround_App_AppHost>(TestContext.Current.CancellationToken);

        var keycloak = appHost.Resources.Single(resource => resource.Name == "keycloak");

        Assert.Contains(keycloak.Annotations.OfType<ContainerMountAnnotation>(), mount => mount.Type == ContainerMountType.Volume);
    }

    [Fact]
    public async Task Api_counts_as_healthy_only_when_its_health_endpoint_answers()
    {
        await using var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.CloudGround_App_AppHost>(TestContext.Current.CancellationToken);

        var api = appHost.Resources.Single(resource => resource.Name == "api");

        Assert.NotEmpty(api.Annotations.OfType<HealthCheckAnnotation>());
    }

    [Fact]
    public async Task Web_counts_as_healthy_only_when_its_health_endpoint_answers()
    {
        await using var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.CloudGround_App_AppHost>(TestContext.Current.CancellationToken);

        var web = appHost.Resources.Single(resource => resource.Name == "web");

        Assert.NotEmpty(web.Annotations.OfType<HealthCheckAnnotation>());
    }

    [Fact]
    public async Task Tests_run_two_bff_instances()
    {
        await using var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.CloudGround_App_AppHost>(TestContext.Current.CancellationToken);

        appHost.WithTwoBffInstances();

        var bff = appHost.Resources.Single(resource => resource.Name == "bff");
        Assert.Equal(BffInstances.Count, Assert.Single(bff.Annotations.OfType<ReplicaAnnotation>()).Replicas);
    }

    [Fact]
    public async Task Web_app_loads_its_telemetry_instrumentation()
    {
        var cancellation = TestContext.Current.CancellationToken;
        await using var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.CloudGround_App_AppHost>(cancellation);
        var web = appHost.Resources.Single(resource => resource.Name == "web");
        await using var app = await appHost.BuildAsync(cancellation);
        var environment = new EnvironmentCallbackContext(app.Services.GetRequiredService<DistributedApplicationExecutionContext>(), web, [], cancellation);

        foreach (var callback in web.Annotations.OfType<EnvironmentCallbackAnnotation>())
        {
            await callback.Callback(environment);
        }

        Assert.Contains("--import ./src/instrumentation.mjs", environment.EnvironmentVariables["NODE_OPTIONS"].ToString(), StringComparison.Ordinal);
        Assert.True(environment.EnvironmentVariables.ContainsKey("OTEL_EXPORTER_OTLP_ENDPOINT"));
    }

    [Fact]
    public async Task Every_module_applies_its_migrations_before_the_api_starts()
    {
        await using var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.CloudGround_App_AppHost>(TestContext.Current.CancellationToken);

        var api = appHost.Resources.Single(resource => resource.Name == "api");

        foreach (var module in ModuleNames(appHost.AppHostDirectory))
        {
            var migrations = Assert.Single(appHost.Resources.OfType<EFMigrationResource>(), resource => resource.Name == $"{module.ToLowerInvariant()}-migrations");

            Assert.Contains(api.Annotations.OfType<WaitAnnotation>(), wait => wait.Resource == migrations && wait.WaitType == WaitType.WaitForCompletion);
        }
    }

    [Fact]
    public async Task Module_migrations_run_one_after_another()
    {
        await using var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.CloudGround_App_AppHost>(TestContext.Current.CancellationToken);

        var migrations = appHost.Resources.OfType<EFMigrationResource>().ToList();

        foreach (var (previous, next) in migrations.Zip(migrations.Skip(1)))
        {
            Assert.Contains(next.Annotations.OfType<WaitAnnotation>(), wait => wait.Resource == previous && wait.WaitType == WaitType.WaitForCompletion);
        }
    }

    [Fact]
    public async Task Tests_start_without_data_volumes()
    {
        await using var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.CloudGround_App_AppHost>(TestContext.Current.CancellationToken);

        appHost.WithoutDataVolumes();

        Assert.DoesNotContain(
            appHost.Resources.SelectMany(resource => resource.Annotations.OfType<ContainerMountAnnotation>()),
            mount => mount.Type == ContainerMountType.Volume);
    }

    private static IEnumerable<string> ModuleNames(string appHostDirectory) =>
        new DirectoryInfo(Path.Combine(appHostDirectory, "..", "Modules")) is { Exists: true } modules
            ? modules.EnumerateDirectories(ModulePrefix + "*")
                .Select(directory => directory.Name[ModulePrefix.Length..])
                .Where(name => !name.EndsWith(".Contracts", StringComparison.Ordinal))
            : [];
}
