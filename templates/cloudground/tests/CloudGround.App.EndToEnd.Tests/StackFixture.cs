using CloudGround.App.Integration.Tests;

[assembly: Trait("Category", "EndToEnd")]

namespace CloudGround.App.EndToEnd.Tests;

[CollectionDefinition(Name)]
public sealed class RunningApp : ICollectionFixture<StackFixture>
{
    public const string Name = "RunningApp";
}

public sealed class StackFixture : IAsyncLifetime
{
    private static readonly string[] Resources = ["api", "keycloak", "bff", "web"];

    private DistributedApplication? app;

    public Uri BffAddress => (app ?? throw new InvalidOperationException("The AppHost has not been started.")).GetEndpoint("bff", "http");

    public async ValueTask InitializeAsync()
    {
        Assert.Equal(0, Microsoft.Playwright.Program.Main(["install", "chromium"]));

        var cancellation = TestContext.Current.CancellationToken;
        await using var startLock = await AppHostStartLock.AcquireAsync(cancellation);
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.CloudGround_App_AppHost>(cancellation);
        app = await appHost.WithoutDataVolumes().BuildAsync(cancellation);
        await app.StartAsync(cancellation);
        foreach (var resource in Resources)
        {
            await app.ResourceNotifications.WaitForResourceHealthyAsync(resource, cancellation);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (app is not null)
        {
            await app.DisposeAsync();
        }
    }
}
