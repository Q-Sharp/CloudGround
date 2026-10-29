using Aspire.Hosting.Docker.Resources;

namespace CloudGround.App.AppHost;

internal static class ComposeDeployment
{
    private const string RestartUnlessStopped = "unless-stopped";
    private const string CompletedSuccessfully = "service_completed_successfully";

    public static void RestartLongRunningServices(ComposeFile composeFile)
    {
        var runOnce = composeFile.Services.Values
            .SelectMany(service => service.DependsOn)
            .Where(dependency => dependency.Value.Condition == CompletedSuccessfully)
            .Select(dependency => dependency.Key)
            .ToHashSet();

        foreach (var (name, service) in composeFile.Services)
        {
            if (service.Restart is null && !runOnce.Contains(name))
            {
                service.Restart = RestartUnlessStopped;
            }
        }
    }
}
