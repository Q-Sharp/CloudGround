namespace CloudGround.App.Integration.Tests;

internal static class ApiInstances
{
    public const int Count = 2;

    public static IDistributedApplicationTestingBuilder WithTwoApiInstances(this IDistributedApplicationTestingBuilder appHost)
    {
        var api = appHost.Resources.Single(resource => resource.Name == "api");
        foreach (var replicas in api.Annotations.OfType<ReplicaAnnotation>().ToList())
        {
            api.Annotations.Remove(replicas);
        }

        api.Annotations.Add(new ReplicaAnnotation(Count));
        return appHost;
    }
}
