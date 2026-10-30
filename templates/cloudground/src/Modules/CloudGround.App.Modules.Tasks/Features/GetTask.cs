namespace CloudGround.App.Modules.Tasks.Features;

internal sealed class GetTaskHandler(TasksDbContext database)
{
    public async Task<TaskResponse?> HandleAsync(Guid taskId, CancellationToken cancellationToken) =>
        await database.Tasks
            .AsNoTracking()
            .Where(task => task.Id == taskId)
            .ProjectToResponse()
            .SingleOrDefaultAsync(cancellationToken);
}

internal static class GetTaskEndpoint
{
    public const string RouteName = "GetTask";

    public static RouteGroupBuilder MapGetTask(this RouteGroupBuilder group)
    {
        group.MapGet("/tasks/{id:guid}", GetAsync).WithName(RouteName);
        return group;
    }

    private static async Task<Results<Ok<TaskResponse>, NotFound>> GetAsync(
        Guid id,
        GetTaskHandler handler,
        CancellationToken cancellationToken) =>
        await handler.HandleAsync(id, cancellationToken) is { } task
            ? TypedResults.Ok(task)
            : TypedResults.NotFound();
}
