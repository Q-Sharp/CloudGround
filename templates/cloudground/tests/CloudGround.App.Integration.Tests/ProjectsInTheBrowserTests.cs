using System.Net;
using System.Net.Http.Json;
using static CloudGround.App.Integration.Tests.ModuleAssertions;

namespace CloudGround.App.Integration.Tests;

public sealed class ProjectsInTheBrowserTests(AppHostFixture fixture)
{
    private static readonly Uri ProjectsUri = new("/api/projects", UriKind.Relative);

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Signed_in_write_with_the_xsrf_header_succeeds()
    {
        using var session = await BrowserLogin.LogInAsync(fixture, "alice");

        using var response = await session.Client.SendAsync(Write(session, "Written in the browser", withXsrfHeader: true), Cancellation);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Write_without_xsrf_header_is_rejected()
    {
        using var session = await BrowserLogin.LogInAsync(fixture, "alice");

        using var response = await session.Client.SendAsync(Write(session, "Cross-site attempt", withXsrfHeader: false), Cancellation);

        AssertProblem(response, HttpStatusCode.BadRequest);
        var projects = await session.Client.GetFromJsonAsync<ProjectDto[]>(ProjectsUri, Cancellation);
        Assert.DoesNotContain(projects!, project => project.Name == "Cross-site attempt");
    }

    [Fact]
    public async Task Signed_in_user_without_the_editor_role_is_forbidden()
    {
        using var session = await BrowserLogin.LogInAsync(fixture, "bob");

        using var response = await session.Client.SendAsync(Write(session, "Written by bob in the browser", withXsrfHeader: true), Cancellation);

        AssertProblem(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Bearer_write_needs_no_xsrf_token()
    {
        using var client = await fixture.CreateApiClientAsync("alice");

        using var response = await client.PostAsJsonAsync(ProjectsUri, new { Name = "Written with a bearer token" }, Cancellation);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static HttpRequestMessage Write(BrowserSession session, string name, bool withXsrfHeader)
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
