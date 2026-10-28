[assembly: AssemblyFixture(typeof(CloudGround.App.Integration.Tests.AppHostFixture))]

namespace CloudGround.App.Integration.Tests;

public sealed class AppHostFixture : IAsyncLifetime
{
    private DistributedApplication? app;

    public DistributedApplication App => app ?? throw new InvalidOperationException("The AppHost has not been started.");

    public async ValueTask InitializeAsync()
    {
        var cancellation = TestContext.Current.CancellationToken;
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.CloudGround_App_AppHost>(cancellation);
        app = await appHost.WithoutDataVolumes().BuildAsync(cancellation);
        await app.StartAsync(cancellation);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("api", cancellation);
    }

    public HttpClient CreateApiClient() => App.CreateHttpClient("api", "http");

    public async ValueTask DisposeAsync()
    {
        if (app is not null)
        {
            await app.DisposeAsync();
        }
    }
}
