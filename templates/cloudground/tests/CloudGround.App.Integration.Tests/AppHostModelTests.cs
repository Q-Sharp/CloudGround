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
    public async Task Tests_start_without_data_volumes()
    {
        await using var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.CloudGround_App_AppHost>(TestContext.Current.CancellationToken);

        appHost.WithoutDataVolumes();

        Assert.DoesNotContain(
            appHost.Resources.SelectMany(resource => resource.Annotations.OfType<ContainerMountAnnotation>()),
            mount => mount.Type == ContainerMountType.Volume);
    }
}
