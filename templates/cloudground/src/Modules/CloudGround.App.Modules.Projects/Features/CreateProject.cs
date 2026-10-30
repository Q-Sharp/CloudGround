namespace CloudGround.App.Modules.Projects.Features;

public sealed record CreateProjectRequest(
    [Required, StringLength(Project.NameMaxLength)] string Name,
    [StringLength(Project.DescriptionMaxLength)] string? Description);

internal sealed class CreateProjectHandler(ProjectsDbContext database, TimeProvider clock)
{
    public async Task<ProjectResponse> HandleAsync(CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var project = Project.Create(request.Name, request.Description, clock.GetUtcNow());
        database.Projects.Add(project);
        await database.SaveChangesAsync(cancellationToken);
        return project.ToResponse();
    }
}

internal static class CreateProjectEndpoint
{
    public static RouteGroupBuilder MapCreateProject(this RouteGroupBuilder group)
    {
        group.MapPost("/", CreateAsync).ProducesValidationProblem().RequireAuthorization(ProjectsPolicies.Edit);
        return group;
    }

    private static async Task<CreatedAtRoute<ProjectResponse>> CreateAsync(
        CreateProjectRequest request,
        CreateProjectHandler handler,
        CancellationToken cancellationToken)
    {
        var project = await handler.HandleAsync(request, cancellationToken);
        return TypedResults.CreatedAtRoute(project, GetProjectEndpoint.RouteName, new { id = project.Id });
    }
}
