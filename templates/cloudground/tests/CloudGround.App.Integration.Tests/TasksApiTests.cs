using System.Net;
using System.Net.Http.Json;
using static CloudGround.App.Integration.Tests.ModuleAssertions;

namespace CloudGround.App.Integration.Tests;

public sealed class TasksApiTests(AppHostFixture fixture)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Created_task_can_be_read()
    {
        using var client = fixture.CreateApiClient();
        var projectId = await CreateProjectAsync(client, "Trade fair");
        var created = await CreateTaskAsync(client, projectId, "Book the booth");

        var read = await client.GetFromJsonAsync<TaskDto>(TaskUri(created.Id), Cancellation);

        Assert.Equal(created, read);
        Assert.Equal(projectId, read!.ProjectId);
    }

    [Fact]
    public async Task Project_tasks_are_listed()
    {
        using var client = fixture.CreateApiClient();
        var projectId = await CreateProjectAsync(client, "Customer survey");
        var first = await CreateTaskAsync(client, projectId, "Draft the questions");
        var second = await CreateTaskAsync(client, projectId, "Send the invitations");

        var tasks = await client.GetFromJsonAsync<TaskDto[]>(ProjectTasksUri(projectId), Cancellation);

        Assert.Equal([first, second], tasks!);
    }

    [Fact]
    public async Task Task_for_unknown_project_is_rejected()
    {
        using var client = fixture.CreateApiClient();
        await CreateTaskAsync(client, await CreateProjectAsync(client, "Known project"), "Proves the route exists");

        using var response = await client.PostAsJsonAsync(ProjectTasksUri(Guid.CreateVersion7()), new { Title = "Orphan" }, Cancellation);

        AssertProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Task_for_archived_project_is_rejected()
    {
        using var client = fixture.CreateApiClient();
        var projectId = await CreateProjectAsync(client, "Old campaign");
        using var archive = await client.PostAsync(new Uri($"/api/projects/{projectId}/archive", UriKind.Relative), null, Cancellation);
        Assert.Equal(HttpStatusCode.NoContent, archive.StatusCode);

        using var response = await client.PostAsJsonAsync(ProjectTasksUri(projectId), new { Title = "Too late" }, Cancellation);

        AssertProblem(response, HttpStatusCode.Conflict);
        Assert.Empty((await client.GetFromJsonAsync<TaskDto[]>(ProjectTasksUri(projectId), Cancellation))!);
    }

    [Fact]
    public async Task Blank_title_is_rejected()
    {
        using var client = fixture.CreateApiClient();
        var projectId = await CreateProjectAsync(client, "Quarterly review");

        using var response = await client.PostAsJsonAsync(ProjectTasksUri(projectId), new { Title = "   " }, Cancellation);

        await AssertValidationProblemAsync(response, "Title");
        Assert.Empty((await client.GetFromJsonAsync<TaskDto[]>(ProjectTasksUri(projectId), Cancellation))!);
    }

    [Fact]
    public async Task Completing_twice_returns_conflict()
    {
        using var client = fixture.CreateApiClient();
        var projectId = await CreateProjectAsync(client, "Team offsite");
        var task = await CreateTaskAsync(client, projectId, "Book the venue");
        using var first = await client.PostAsync(CompleteUri(task.Id), null, Cancellation);

        using var second = await client.PostAsync(CompleteUri(task.Id), null, Cancellation);

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        AssertProblem(second, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Tasks_tables_live_in_the_tasks_schema()
    {
        var tables = await DatabaseTables.InSchemaAsync(fixture.App, "tasks");

        Assert.Contains("Tasks", tables);
        Assert.Contains("__EFMigrationsHistory", tables);
    }

    private static async Task<Guid> CreateProjectAsync(HttpClient client, string name)
    {
        using var response = await client.PostAsJsonAsync(new Uri("/api/projects", UriKind.Relative), new { Name = name }, Cancellation);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<IdDto>(Cancellation))!.Id;
    }

    private static async Task<TaskDto> CreateTaskAsync(HttpClient client, Guid projectId, string title)
    {
        using var response = await client.PostAsJsonAsync(ProjectTasksUri(projectId), new { Title = title }, Cancellation);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var task = await response.Content.ReadFromJsonAsync<TaskDto>(Cancellation);
        Assert.NotNull(task);
        Assert.EndsWith($"/api/tasks/{task.Id}", response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        return task;
    }

    private static Uri ProjectTasksUri(Guid projectId) => new($"/api/projects/{projectId}/tasks", UriKind.Relative);

    private static Uri TaskUri(Guid id) => new($"/api/tasks/{id}", UriKind.Relative);

    private static Uri CompleteUri(Guid id) => new($"/api/tasks/{id}/complete", UriKind.Relative);

    private sealed record IdDto(Guid Id);

    private sealed record TaskDto(Guid Id, Guid ProjectId, string Title, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt, DateTimeOffset? ClosedAt);
}
