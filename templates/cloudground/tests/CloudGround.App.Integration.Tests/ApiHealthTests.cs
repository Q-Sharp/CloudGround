using System.Net;

namespace CloudGround.App.Integration.Tests;

public sealed class ApiHealthTests(AppHostFixture fixture)
{
    [Fact]
    public async Task Api_reports_healthy()
    {
        using var client = fixture.CreateApiClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }
}
