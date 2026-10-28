using System.Net;
using System.Net.Http.Json;
using static CloudGround.App.Integration.Tests.ModuleAssertions;

namespace CloudGround.App.Integration.Tests;

public sealed class ProjectsThroughBffTests(AppHostFixture fixture)
{
    private static readonly Uri ProjectsUri = new("/api/projects", UriKind.Relative);

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Anonymous_read_goes_through_the_bff()
    {
        using var client = fixture.App.CreateHttpClient("bff", "http");

        using var response = await client.GetAsync(ProjectsUri, Cancellation);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Signed_in_write_goes_through_the_bff()
    {
        using var session = await BffLogin.LogInAsync(fixture, "alice");

        using var response = await session.Client.SendAsync(Write(session, "Through the BFF", withXsrfHeader: true), Cancellation);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Write_without_xsrf_header_is_rejected()
    {
        using var session = await BffLogin.LogInAsync(fixture, "alice");

        using var response = await session.Client.SendAsync(Write(session, "Cross-site attempt", withXsrfHeader: false), Cancellation);

        AssertProblem(response, HttpStatusCode.BadRequest);
        var projects = await session.Client.GetFromJsonAsync<ProjectDto[]>(ProjectsUri, Cancellation);
        Assert.DoesNotContain(projects!, project => project.Name == "Cross-site attempt");
    }

    [Fact]
    public async Task Anonymous_write_through_the_bff_returns_401()
    {
        using var client = fixture.App.CreateHttpClient("bff", "http");

        using var response = await client.PostAsJsonAsync(ProjectsUri, new { Name = "Anonymous" }, Cancellation);

        AssertProblem(response, HttpStatusCode.Unauthorized);
    }

    private static HttpRequestMessage Write(BffSession session, string name, bool withXsrfHeader)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, ProjectsUri) { Content = JsonContent.Create(new { Name = name }) };
        if (withXsrfHeader)
        {
            request.Headers.Add("X-XSRF-TOKEN", session.XsrfToken);
        }

        return request;
    }

    private sealed record ProjectDto(Guid Id, string Name);
}
