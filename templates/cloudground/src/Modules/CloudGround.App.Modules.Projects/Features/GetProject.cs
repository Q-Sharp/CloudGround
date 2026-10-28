using CloudGround.App.Modules.Projects.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace CloudGround.App.Modules.Projects.Features;

internal sealed class GetProjectHandler(ProjectsDbContext database, HybridCache cache)
{
    public async Task<ProjectResponse?> HandleAsync(Guid projectId, CancellationToken cancellationToken) =>
        await cache.GetOrCreateAsync(
            ProjectCache.Key(projectId),
            async token => await database.Projects
                .AsNoTracking()
                .Where(project => project.Id == projectId)
                .ProjectToResponse()
                .SingleOrDefaultAsync(token),
            ProjectCache.SharedAcrossInstances,
            cancellationToken: cancellationToken);
}

internal static class GetProjectEndpoint
{
    public const string RouteName = "GetProject";

    public static RouteGroupBuilder MapGetProject(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", GetAsync).WithName(RouteName);
        return group;
    }

    private static async Task<Results<Ok<ProjectResponse>, NotFound>> GetAsync(
        Guid id,
        GetProjectHandler handler,
        CancellationToken cancellationToken) =>
        await handler.HandleAsync(id, cancellationToken) is { } project
            ? TypedResults.Ok(project)
            : TypedResults.NotFound();
}
