using System.Net;
using System.Net.Http.Json;

namespace CloudGround.App.Integration.Tests;

public sealed class ProjectArchivedFlowTests(AppHostFixture fixture)
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(30);

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Archiving_a_project_closes_its_open_tasks()
    {
        using var client = await fixture.CreateApiClientAsync("alice");
        var projectId = await PostForIdAsync(client, "/api/projects", new { Name = "Product launch" });
        var openTaskId = await PostForIdAsync(client, $"/api/projects/{projectId}/tasks", new { Title = "Write the press release" });
        var completedTaskId = await PostForIdAsync(client, $"/api/projects/{projectId}/tasks", new { Title = "Book the venue" });
        using var complete = await client.PostAsync(new Uri($"/api/tasks/{completedTaskId}/complete", UriKind.Relative), null, Cancellation);
        Assert.Equal(HttpStatusCode.NoContent, complete.StatusCode);

        using var archive = await client.PostAsync(new Uri($"/api/projects/{projectId}/archive", UriKind.Relative), null, Cancellation);
        Assert.Equal(HttpStatusCode.NoContent, archive.StatusCode);

        var tasks = await WaitForTasksAsync(client, projectId, tasks => tasks.Single(task => task.Id == openTaskId).ClosedAt is not null);
        Assert.Null(tasks.Single(task => task.Id == completedTaskId).ClosedAt);
        Assert.NotNull(tasks.Single(task => task.Id == completedTaskId).CompletedAt);
    }

    private static async Task<Guid> PostForIdAsync(HttpClient client, string path, object body)
    {
        using var response = await client.PostAsJsonAsync(new Uri(path, UriKind.Relative), body, Cancellation);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<IdDto>(Cancellation))!.Id;
    }

    private static async Task<TaskDto[]> WaitForTasksAsync(HttpClient client, Guid projectId, Func<TaskDto[], bool> condition)
    {
        var deadline = DateTimeOffset.UtcNow + Patience;
        while (true)
        {
            var tasks = (await client.GetFromJsonAsync<TaskDto[]>(new Uri($"/api/projects/{projectId}/tasks", UriKind.Relative), Cancellation))!;
            if (condition(tasks))
            {
                return tasks;
            }

            Assert.True(DateTimeOffset.UtcNow < deadline, $"The tasks of project {projectId} did not reach the expected state within {Patience}.");
            await Task.Delay(TimeSpan.FromMilliseconds(250), Cancellation);
        }
    }

    private sealed record IdDto(Guid Id);

    private sealed record TaskDto(Guid Id, DateTimeOffset? CompletedAt, DateTimeOffset? ClosedAt);
}
