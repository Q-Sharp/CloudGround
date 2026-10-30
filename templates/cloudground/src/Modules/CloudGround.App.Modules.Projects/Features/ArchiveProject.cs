namespace CloudGround.App.Modules.Projects.Features;

internal enum ArchiveProjectOutcome
{
    Archived,
    NotFound,
    AlreadyArchived,
}

internal sealed class ArchiveProjectHandler(IDbContextOutbox<ProjectsDbContext> outbox, HybridCache cache, TimeProvider clock)
{
    public async Task<ArchiveProjectOutcome> HandleAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await outbox.DbContext.Projects.FindAsync([projectId], cancellationToken);
        if (project is null)
        {
            return ArchiveProjectOutcome.NotFound;
        }

        if (!project.TryArchive(clock.GetUtcNow()))
        {
            return ArchiveProjectOutcome.AlreadyArchived;
        }

        try
        {
            await outbox.DbContext.Database.CreateExecutionStrategy()
                .ExecuteAsync(token => SaveAndPublishAsync(new ProjectArchived(project.Id, project.ArchivedAt!.Value), token), cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ArchiveProjectOutcome.AlreadyArchived;
        }

        await cache.RemoveAsync(ProjectCache.Key(projectId), cancellationToken);
        return ArchiveProjectOutcome.Archived;
    }

    private async Task SaveAndPublishAsync(ProjectArchived archived, CancellationToken cancellationToken)
    {
        await using var transaction = await outbox.DbContext.Database.BeginTransactionAsync(cancellationToken);
        await outbox.PublishAsync(archived);
        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
    }
}

internal static class ArchiveProjectEndpoint
{
    public static RouteGroupBuilder MapArchiveProject(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/archive", ArchiveAsync).RequireAuthorization(ProjectsPolicies.Edit);
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
