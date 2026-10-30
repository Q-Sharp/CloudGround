namespace CloudGround.App.Modules.Projects.Features;

internal sealed class ListProjectsHandler(ProjectsDbContext database)
{
    public async Task<List<ProjectResponse>> HandleAsync(CancellationToken cancellationToken) =>
        await database.Projects
            .AsNoTracking()
            .OrderBy(project => project.Name)
            .ProjectToResponse()
            .ToListAsync(cancellationToken);
}

internal static class ListProjectsEndpoint
{
    public static RouteGroupBuilder MapListProjects(this RouteGroupBuilder group)
    {
        group.MapGet("/", ListAsync);
        return group;
    }

    private static async Task<Ok<List<ProjectResponse>>> ListAsync(ListProjectsHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(cancellationToken));
}
