namespace CloudGround.App.Modules.Tasks.Tests;

public sealed class TaskItemTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 28, 9, 0, 0, TimeSpan.Zero);

    private static readonly Guid ProjectId = Guid.CreateVersion7(Now);

    [Fact]
    public void Create_sets_project_title_and_creation_time()
    {
        var task = TaskItem.Create(ProjectId, "Write the brief", Now);

        Assert.Equal(ProjectId, task.ProjectId);
        Assert.Equal("Write the brief", task.Title);
        Assert.Equal(Now, task.CreatedAt);
        Assert.Null(task.CompletedAt);
        Assert.Null(task.ClosedAt);
    }

    [Fact]
    public void Create_assigns_a_version_7_id()
    {
        var task = TaskItem.Create(ProjectId, "Write the brief", Now);

        Assert.Equal(7, task.Id.Version);
    }

    [Fact]
    public void TryComplete_sets_completion_time()
    {
        var task = TaskItem.Create(ProjectId, "Write the brief", Now);

        Assert.True(task.TryComplete(Now.AddHours(1)));
        Assert.Equal(Now.AddHours(1), task.CompletedAt);
    }

    [Fact]
    public void TryComplete_on_completed_task_returns_false_and_keeps_first_time()
    {
        var task = TaskItem.Create(ProjectId, "Write the brief", Now);
        task.TryComplete(Now.AddHours(1));

        Assert.False(task.TryComplete(Now.AddHours(2)));
        Assert.Equal(Now.AddHours(1), task.CompletedAt);
    }

    [Fact]
    public void TryComplete_on_closed_task_returns_false()
    {
        var task = TaskItem.Create(ProjectId, "Write the brief", Now);
        task.Close(Now.AddHours(1));

        Assert.False(task.TryComplete(Now.AddHours(2)));
        Assert.Null(task.CompletedAt);
    }

    [Fact]
    public void Close_sets_closing_time_on_open_task()
    {
        var task = TaskItem.Create(ProjectId, "Write the brief", Now);

        task.Close(Now.AddHours(1));

        Assert.Equal(Now.AddHours(1), task.ClosedAt);
    }

    [Fact]
    public void Close_leaves_a_completed_task_unchanged()
    {
        var task = TaskItem.Create(ProjectId, "Write the brief", Now);
        task.TryComplete(Now.AddHours(1));

        task.Close(Now.AddHours(2));

        Assert.Equal(Now.AddHours(1), task.CompletedAt);
        Assert.Null(task.ClosedAt);
    }

    [Fact]
    public void Closing_twice_keeps_the_first_closing_time()
    {
        var task = TaskItem.Create(ProjectId, "Write the brief", Now);
        task.Close(Now.AddHours(1));

        task.Close(Now.AddHours(2));

        Assert.Equal(Now.AddHours(1), task.ClosedAt);
    }

    [Fact]
    public void Timestamps_are_kept_at_microsecond_precision()
    {
        var task = TaskItem.Create(ProjectId, "Write the brief", Now.AddTicks(7));

        task.TryComplete(Now.AddHours(1).AddTicks(3));

        Assert.Equal(Now, task.CreatedAt);
        Assert.Equal(Now.AddHours(1), task.CompletedAt);
    }
}
