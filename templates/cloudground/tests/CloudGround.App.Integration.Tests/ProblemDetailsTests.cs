using System.Net;

namespace CloudGround.App.Integration.Tests;

public sealed class ProblemDetailsTests(AppHostFixture fixture)
{
    [Fact]
    public async Task Unknown_route_returns_problem_details()
    {
        using var client = fixture.CreateApiClient();

        using var response = await client.GetAsync(new Uri("/api/does-not-exist", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
