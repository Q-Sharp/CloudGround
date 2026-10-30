using System.Net;
using System.Net.Http.Json;

namespace CloudGround.App.Integration.Tests;

public sealed class ProjectsWebTests(AppHostFixture fixture)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Explore_page_lists_projects_rendered_on_the_server()
    {
        using var api = await fixture.CreateApiClientAsync("alice");
        using var created = await api.PostAsJsonAsync(new Uri("/api/projects", UriKind.Relative), new { Name = "Rendered on the server" }, Cancellation);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var browser = fixture.CreateApiClient();

        var page = await browser.GetStringAsync(new Uri("/explore", UriKind.Relative), Cancellation);

        Assert.Contains("Rendered on the server", page, StringComparison.Ordinal);
    }
}
