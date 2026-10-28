using System.Net;

namespace CloudGround.App.Integration.Tests;

public sealed class WebTests(AppHostFixture fixture)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Home_page_is_server_rendered_through_the_bff()
    {
        using var client = fixture.App.CreateHttpClient("bff", "http");

        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), Cancellation);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("ng-server-context=\"ssr\"", await response.Content.ReadAsStringAsync(Cancellation), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unknown_paths_return_the_web_apps_not_found_page()
    {
        using var client = fixture.App.CreateHttpClient("bff", "http");

        using var response = await client.GetAsync(new Uri("/no/such/page", UriKind.Relative), Cancellation);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }
}
