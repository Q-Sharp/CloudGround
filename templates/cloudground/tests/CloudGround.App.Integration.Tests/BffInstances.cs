using Aspire.Hosting.ApplicationModel;

namespace CloudGround.App.Integration.Tests;

internal static class BffInstances
{
    public const int Count = 2;

    public static IDistributedApplicationTestingBuilder WithTwoBffInstances(this IDistributedApplicationTestingBuilder appHost)
    {
        var bff = appHost.Resources.Single(resource => resource.Name == "bff");
        foreach (var replicas in bff.Annotations.OfType<ReplicaAnnotation>().ToList())
        {
            bff.Annotations.Remove(replicas);
        }

        bff.Annotations.Add(new ReplicaAnnotation(Count));
        return appHost;
    }
}
