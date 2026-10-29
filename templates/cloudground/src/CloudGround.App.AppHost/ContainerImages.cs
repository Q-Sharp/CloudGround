using System.Reflection;

namespace CloudGround.App.AppHost;

internal static class ContainerImages
{
    public static IDistributedApplicationBuilder TagImagesWithVersion(this IDistributedApplicationBuilder builder)
    {
        var version = typeof(ContainerImages).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
            .InformationalVersion
            .Split('+')[0];
        var images = builder.Resources.OfType<IComputeResource>().Where(resource => resource.RequiresImageBuild()).ToList();

#pragma warning disable ASPIREPIPELINES003
        foreach (var image in images)
        {
            builder.CreateResourceBuilder(image).WithRemoteImageTag(version);
        }
#pragma warning restore ASPIREPIPELINES003

        return builder;
    }
}
