namespace CloudGround.App.Integration.Tests;

public sealed class OpenApiTests(AppHostFixture fixture)
{
    [Fact]
    public async Task Api_serves_openapi_document()
    {
        using var client = fixture.CreateApiClient();

        using var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"openapi\"", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Api_serves_the_scalar_ui_in_development()
    {
        using var client = fixture.CreateApiClient();

        using var response = await client.GetAsync(new Uri("/scalar", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }
}
