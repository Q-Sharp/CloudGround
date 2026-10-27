using System.Net;

namespace CloudGround.App.Integration.Tests;

public sealed class ApiHealthTests
{
    [Fact]
    public async Task Api_reports_healthy()
    {
        var cancellation = TestContext.Current.CancellationToken;
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.CloudGround_App_AppHost>(cancellation);
        await using var app = await appHost.BuildAsync(cancellation);
        await app.StartAsync(cancellation);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("api", cancellation);

        using var client = app.CreateHttpClient("api", "http");
        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative), cancellation);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(cancellation));
    }
}
