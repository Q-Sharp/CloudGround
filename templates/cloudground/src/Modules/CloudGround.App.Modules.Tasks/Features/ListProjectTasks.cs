namespace CloudGround.App.Modules.Tasks.Features;

internal sealed class ListProjectTasksHandler(TasksDbContext database)
{
    public async Task<List<TaskResponse>> HandleAsync(Guid projectId, CancellationToken cancellationToken) =>
        await database.Tasks
            .AsNoTracking()
            .Where(task => task.ProjectId == projectId)
            .OrderBy(task => task.CreatedAt)
            .ThenBy(task => task.Id)
            .ProjectToResponse()
            .ToListAsync(cancellationToken);
}

internal static class ListProjectTasksEndpoint
{
    public static RouteGroupBuilder MapListProjectTasks(this RouteGroupBuilder group)
    {
        group.MapGet("/projects/{projectId:guid}/tasks", ListAsync);
        return group;
    }

    private static async Task<Ok<List<TaskResponse>>> ListAsync(
        Guid projectId,
        ListProjectTasksHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(projectId, cancellationToken));
}
