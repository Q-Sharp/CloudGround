namespace CloudGround.App.Modules.Tasks.Tests;

public sealed class ProjectArchivedHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 28, 9, 0, 0, TimeSpan.Zero);

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Handling_closes_only_the_open_tasks_of_the_archived_project()
    {
        await using var database = TasksDatabase.Create();
        var projectId = Guid.CreateVersion7(Now);
        var open = TaskItem.Create(projectId, "Write the press release", Now);
        var completed = TaskItem.Create(projectId, "Book the venue", Now);
        completed.TryComplete(Now.AddHours(1));
        var otherProject = TaskItem.Create(Guid.CreateVersion7(Now), "Unrelated", Now);
        database.Tasks.AddRange(open, completed, otherProject);
        await database.SaveChangesAsync(Cancellation);

        await ProjectArchivedHandler.HandleAsync(new ProjectArchived(projectId, Now.AddDays(1)), new CloseProjectTasks(database, new FixedClock(Now.AddDays(1))), Cancellation);

        var tasks = await database.Tasks.AsNoTracking().ToDictionaryAsync(task => task.Id, Cancellation);
        Assert.Equal(Now.AddDays(1), tasks[open.Id].ClosedAt);
        Assert.Null(tasks[completed.Id].ClosedAt);
        Assert.Null(tasks[otherProject.Id].ClosedAt);
    }

    [Fact]
    public async Task Handling_the_same_event_twice_keeps_the_first_closing_time()
    {
        await using var database = TasksDatabase.Create();
        var projectId = Guid.CreateVersion7(Now);
        var task = TaskItem.Create(projectId, "Write the press release", Now);
        database.Tasks.Add(task);
        await database.SaveChangesAsync(Cancellation);
        var archived = new ProjectArchived(projectId, Now.AddDays(1));
        var clock = new FixedClock(Now.AddDays(1));
        var tasks = new CloseProjectTasks(database, clock);

        await ProjectArchivedHandler.HandleAsync(archived, tasks, Cancellation);
        clock.Now = Now.AddDays(2);
        await ProjectArchivedHandler.HandleAsync(archived, tasks, Cancellation);

        Assert.Equal(Now.AddDays(1), (await database.Tasks.AsNoTracking().SingleAsync(Cancellation)).ClosedAt);
    }
}
