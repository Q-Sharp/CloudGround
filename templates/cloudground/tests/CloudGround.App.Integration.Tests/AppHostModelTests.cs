using Aspire.Hosting.ApplicationModel;

namespace CloudGround.App.Integration.Tests;

public sealed class AppHostModelTests
{
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
    public async Task Tests_run_two_bff_instances()
    {
        await using var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.CloudGround_App_AppHost>(TestContext.Current.CancellationToken);

        appHost.WithTwoBffInstances();

        var bff = appHost.Resources.Single(resource => resource.Name == "bff");
        Assert.Equal(BffInstances.Count, Assert.Single(bff.Annotations.OfType<ReplicaAnnotation>()).Replicas);
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
}
