using CloudGround.App.Modules.Tasks.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CloudGround.App.Modules.Tasks.Features;

internal enum CompleteTaskOutcome
{
    Completed,
    NotFound,
    NotOpen,
}

internal sealed class CompleteTaskHandler(TasksDbContext database, TimeProvider clock)
{
    public async Task<CompleteTaskOutcome> HandleAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var task = await database.Tasks.FindAsync([taskId], cancellationToken);
        if (task is null)
        {
            return CompleteTaskOutcome.NotFound;
        }

        if (!task.TryComplete(clock.GetUtcNow()))
        {
            return CompleteTaskOutcome.NotOpen;
        }

        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return CompleteTaskOutcome.NotOpen;
        }

        return CompleteTaskOutcome.Completed;
    }
}

internal static class CompleteTaskEndpoint
{
    public static RouteGroupBuilder MapCompleteTask(this RouteGroupBuilder group)
    {
        group.MapPost("/tasks/{id:guid}/complete", CompleteAsync);
        return group;
    }

    private static async Task<Results<NoContent, NotFound, Conflict>> CompleteAsync(
        Guid id,
        CompleteTaskHandler handler,
        CancellationToken cancellationToken) =>
        await handler.HandleAsync(id, cancellationToken) switch
        {
            CompleteTaskOutcome.Completed => TypedResults.NoContent(),
            CompleteTaskOutcome.NotFound => TypedResults.NotFound(),
            _ => TypedResults.Conflict(),
        };
}
