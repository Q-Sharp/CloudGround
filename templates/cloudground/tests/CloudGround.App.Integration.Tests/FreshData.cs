namespace CloudGround.App.Integration.Tests;

internal static class FreshData
{
    public static IDistributedApplicationTestingBuilder WithoutDataVolumes(this IDistributedApplicationTestingBuilder appHost)
    {
        foreach (var resource in appHost.Resources)
        {
            var volumes = resource.Annotations
                .OfType<ContainerMountAnnotation>()
                .Where(mount => mount.Type == ContainerMountType.Volume)
                .ToList();

            volumes.ForEach(volume => resource.Annotations.Remove(volume));
        }

        return appHost;
    }
}
