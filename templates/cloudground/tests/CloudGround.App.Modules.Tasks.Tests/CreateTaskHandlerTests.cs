using CloudGround.App.Modules.Projects.Contracts;
using CloudGround.App.Modules.Tasks.Features;
using Microsoft.EntityFrameworkCore;

namespace CloudGround.App.Modules.Tasks.Tests;

public sealed class CreateTaskHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 28, 9, 0, 0, TimeSpan.Zero);

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Task_created_while_its_project_is_archived_ends_up_closed()
    {
        await using var database = TasksDatabase.Create();
        var projectId = Guid.CreateVersion7(Now);
        var catalog = new ProjectArchivedAfterFirstLookup(projectId);
        var handler = new CreateTaskHandler(database, catalog, new FixedClock(Now));

        var result = await handler.HandleAsync(projectId, new CreateTaskRequest("Write the press release"), Cancellation);

        var created = Assert.IsType<CreateTaskResult.Created>(result);
        Assert.Equal(Now, created.Task.ClosedAt);
        Assert.Equal(Now, (await database.Tasks.AsNoTracking().SingleAsync(Cancellation)).ClosedAt);
    }

    private sealed class ProjectArchivedAfterFirstLookup(Guid projectId) : IProjectCatalog
    {
        private int lookups;

        public Task<ProjectStatus?> FindAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<ProjectStatus?>(new ProjectStatus(projectId, IsArchived: lookups++ > 0));
    }
}
