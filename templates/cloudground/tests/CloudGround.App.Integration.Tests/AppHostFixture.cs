[assembly: AssemblyFixture(typeof(CloudGround.App.Integration.Tests.AppHostFixture))]

namespace CloudGround.App.Integration.Tests;

public sealed class AppHostFixture : IAsyncLifetime
{
    private DistributedApplication? app;

    public DistributedApplication App => app ?? throw new InvalidOperationException("The AppHost has not been started.");

    public async ValueTask InitializeAsync()
    {
        var cancellation = TestContext.Current.CancellationToken;
        await using var startLock = await AppHostStartLock.AcquireAsync(cancellation);
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.CloudGround_App_AppHost>(cancellation);
        app = await appHost.WithoutDataVolumes().WithTwoApiInstances().BuildAsync(cancellation);
        await app.StartAsync(cancellation);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("api", cancellation);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("keycloak", cancellation);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("web", cancellation);
    }

    public HttpClient CreateApiClient() => App.CreateHttpClient("api", "http");

    public async Task<HttpClient> CreateApiClientAsync(string user)
    {
        var client = CreateApiClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", await Tokens.ForAsync(App, user, TestContext.Current.CancellationToken));
        return client;
    }

    public async ValueTask DisposeAsync()
    {
        if (app is not null)
        {
            await app.DisposeAsync();
        }
    }
}
