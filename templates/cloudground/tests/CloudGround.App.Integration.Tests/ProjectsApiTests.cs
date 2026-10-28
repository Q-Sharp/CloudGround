using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Npgsql;

namespace CloudGround.App.Integration.Tests;

public sealed class ProjectsApiTests(AppHostFixture fixture)
{
    private static readonly Uri ProjectsUri = new("/api/projects", UriKind.Relative);

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Created_project_can_be_read()
    {
        using var client = fixture.CreateApiClient();
        var created = await CreateProjectAsync(client, "Website relaunch");

        var read = await client.GetFromJsonAsync<ProjectDto>(ProjectUri(created.Id), Cancellation);

        Assert.Equal(created, read);
    }

    [Fact]
    public async Task Created_project_appears_in_the_list()
    {
        using var client = fixture.CreateApiClient();
        var created = await CreateProjectAsync(client, "Annual report");

        var projects = await client.GetFromJsonAsync<ProjectDto[]>(ProjectsUri, Cancellation);

        Assert.Contains(created, projects!);
    }

    [Fact]
    public async Task Blank_name_is_rejected()
    {
        using var client = fixture.CreateApiClient();

        using var response = await client.PostAsJsonAsync(ProjectsUri, new { Name = "   " }, Cancellation);

        await AssertValidationProblemAsync(response, "Name");
        await AssertNoProjectNamedAsync(client, "   ");
    }

    [Fact]
    public async Task Too_long_name_is_rejected()
    {
        using var client = fixture.CreateApiClient();

        var name = new string('x', 101);

        using var response = await client.PostAsJsonAsync(ProjectsUri, new { Name = name }, Cancellation);

        await AssertValidationProblemAsync(response, "Name");
        await AssertNoProjectNamedAsync(client, name);
    }

    [Fact]
    public async Task Malformed_json_returns_problem_details()
    {
        using var client = fixture.CreateApiClient();
        using var body = new StringContent("{", Encoding.UTF8, "application/json");

        using var response = await client.PostAsync(ProjectsUri, body, Cancellation);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Unknown_project_returns_problem_details()
    {
        using var client = fixture.CreateApiClient();

        using var response = await client.GetAsync(ProjectUri(Guid.CreateVersion7()), Cancellation);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Archived_project_is_returned_as_archived_after_a_cached_read()
    {
        using var client = fixture.CreateApiClient();
        var created = await CreateProjectAsync(client, "Office move");
        await client.GetFromJsonAsync<ProjectDto>(ProjectUri(created.Id), Cancellation);

        using var archive = await client.PostAsync(ArchiveUri(created.Id), null, Cancellation);
        var read = await client.GetFromJsonAsync<ProjectDto>(ProjectUri(created.Id), Cancellation);

        Assert.Equal(HttpStatusCode.NoContent, archive.StatusCode);
        Assert.NotNull(read!.ArchivedAt);
    }

    [Fact]
    public async Task Archiving_twice_returns_conflict()
    {
        using var client = fixture.CreateApiClient();
        var created = await CreateProjectAsync(client, "Hiring round");
        using var first = await client.PostAsync(ArchiveUri(created.Id), null, Cancellation);

        using var second = await client.PostAsync(ArchiveUri(created.Id), null, Cancellation);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("application/problem+json", second.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Concurrent_archives_conflict()
    {
        using var client = fixture.CreateApiClient();
        var created = await CreateProjectAsync(client, "Data center exit");

        var responses = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => client.PostAsync(ArchiveUri(created.Id), null, Cancellation)));
        var statuses = responses.Select(response => response.StatusCode).ToList();
        Array.ForEach(responses, response => response.Dispose());

        Assert.Single(statuses, HttpStatusCode.NoContent);
        Assert.All(statuses.Where(status => status != HttpStatusCode.NoContent), status => Assert.Equal(HttpStatusCode.Conflict, status));
    }

    [Fact]
    public async Task Api_documents_validation_errors_for_creating_a_project()
    {
        using var client = fixture.CreateApiClient();

        using var document = JsonDocument.Parse(await client.GetStringAsync(new Uri("/openapi/v1.json", UriKind.Relative), Cancellation));
        var responses = document.RootElement.GetProperty("paths").GetProperty("/api/projects").GetProperty("post").GetProperty("responses");

        Assert.True(responses.TryGetProperty("400", out _));
    }

    [Fact]
    public async Task Projects_tables_live_in_the_projects_schema()
    {
        var connectionString = await fixture.App.GetConnectionStringAsync("database", Cancellation);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(Cancellation);
        await using var command = new NpgsqlCommand("select table_name from information_schema.tables where table_schema = 'projects'", connection);

        var tables = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(Cancellation);
        while (await reader.ReadAsync(Cancellation))
        {
            tables.Add(reader.GetString(0));
        }

        Assert.Contains("Projects", tables);
        Assert.Contains("__EFMigrationsHistory", tables);
    }

    private static async Task<ProjectDto> CreateProjectAsync(HttpClient client, string name)
    {
        using var response = await client.PostAsJsonAsync(ProjectsUri, new { Name = name, Description = "Created by a test" }, Cancellation);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var project = await response.Content.ReadFromJsonAsync<ProjectDto>(Cancellation);
        Assert.NotNull(project);
        Assert.EndsWith($"/api/projects/{project.Id}", response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        return project;
    }

    private static async Task AssertValidationProblemAsync(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Cancellation));
        var errors = problem.RootElement.GetProperty("errors").EnumerateObject().Select(error => error.Name);
        Assert.Contains(errors, error => error.Equals(field, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task AssertNoProjectNamedAsync(HttpClient client, string name)
    {
        var projects = await client.GetFromJsonAsync<ProjectDto[]>(ProjectsUri, Cancellation);
        Assert.DoesNotContain(projects!, project => project.Name == name);
    }

    private static Uri ProjectUri(Guid id) => new($"/api/projects/{id}", UriKind.Relative);

    private static Uri ArchiveUri(Guid id) => new($"/api/projects/{id}/archive", UriKind.Relative);

    private sealed record ProjectDto(Guid Id, string Name, string? Description, DateTimeOffset CreatedAt, DateTimeOffset? ArchivedAt);
}
