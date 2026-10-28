using CloudGround.App.Modules.Projects.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Hybrid;

namespace CloudGround.App.Modules.Projects.Features;

internal enum ArchiveProjectOutcome
{
    Archived,
    NotFound,
    AlreadyArchived,
}

internal sealed class ArchiveProjectHandler(ProjectsDbContext database, HybridCache cache, TimeProvider clock)
{
    public async Task<ArchiveProjectOutcome> HandleAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await database.Projects.FindAsync([projectId], cancellationToken);
        if (project is null)
        {
            return ArchiveProjectOutcome.NotFound;
        }

        if (!project.TryArchive(clock.GetUtcNow()))
        {
            return ArchiveProjectOutcome.AlreadyArchived;
        }

        await database.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync(ProjectCache.Key(projectId), cancellationToken);
        return ArchiveProjectOutcome.Archived;
    }
}

internal static class ArchiveProjectEndpoint
{
    public static RouteGroupBuilder MapArchiveProject(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/archive", ArchiveAsync);
        return group;
    }

    private static async Task<Results<NoContent, NotFound, Conflict>> ArchiveAsync(
        Guid id,
        ArchiveProjectHandler handler,
        CancellationToken cancellationToken) =>
        await handler.HandleAsync(id, cancellationToken) switch
        {
            ArchiveProjectOutcome.Archived => TypedResults.NoContent(),
            ArchiveProjectOutcome.NotFound => TypedResults.NotFound(),
            _ => TypedResults.Conflict(),
        };
}
