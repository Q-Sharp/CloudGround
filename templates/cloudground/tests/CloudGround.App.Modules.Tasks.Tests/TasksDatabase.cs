namespace CloudGround.App.Modules.Tasks.Tests;

internal static class TasksDatabase
{
    public static TasksDbContext Create() =>
        new(new DbContextOptionsBuilder<TasksDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
