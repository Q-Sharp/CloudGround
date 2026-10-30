using System.Globalization;
using Aspire.Hosting.Docker;
using Aspire.Hosting.Docker.Resources;
using Aspire.Hosting.Docker.Resources.ServiceNodes;

namespace CloudGround.App.AppHost;

internal static class ComposeDeployment
{
    private const string ProjectName = "cloudground-app";
    private const string Loopback = "127.0.0.1";
    private const string RestartUnlessStopped = "unless-stopped";
    private const string Started = "service_started";
    private const string Healthy = "service_healthy";
    private const string CompletedSuccessfully = "service_completed_successfully";
    private const string ProbeInterval = "5s";
    private const string ProbeStartPeriod = "10s";
    private const int ProbeRetries = 30;
    private const string DashboardImage = "mcr.microsoft.com/dotnet/aspire-dashboard";

    public static void Configure(ComposeFile composeFile)
    {
        composeFile.Name = ProjectName;
        PublishPortsOnLoopback(composeFile);
        RestartLongRunningServices(composeFile);
        WaitForHealthyDependencies(composeFile);
    }

    public static void UseReleasedDashboard(IResourceBuilder<DockerComposeAspireDashboardResource> dashboard)
    {
        var aspire = typeof(DockerComposeEnvironmentResource).Assembly.GetName().Version!;
        dashboard.WithImage(DashboardImage, aspire.Major.ToString(CultureInfo.InvariantCulture));
    }

    public static Healthcheck Probe(params string[] command) => new()
    {
        Test = ["CMD", .. command],
        Interval = ProbeInterval,
        Timeout = ProbeInterval,
        Retries = ProbeRetries,
        StartPeriod = ProbeStartPeriod,
    };

    private static void PublishPortsOnLoopback(ComposeFile composeFile)
    {
        foreach (var service in composeFile.Services.Values)
        {
            service.Ports = [.. service.Ports.Select(port => port.Contains(':', StringComparison.Ordinal) ? port : $"{Loopback}:{port}:{port}")];
        }
    }

    private static void RestartLongRunningServices(ComposeFile composeFile)
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

    private static void WaitForHealthyDependencies(ComposeFile composeFile)
    {
        var dependencies = composeFile.Services.Values.SelectMany(service => service.DependsOn);
        foreach (var (name, dependency) in dependencies)
        {
            if (dependency.Condition == Started && composeFile.Services.TryGetValue(name, out var target) && target.Healthcheck is not null)
            {
                dependency.Condition = Healthy;
            }
        }
    }
}
