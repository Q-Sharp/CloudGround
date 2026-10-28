using System.ComponentModel.DataAnnotations;
using CloudGround.App.Modules.Projects.Contracts;
using CloudGround.App.Modules.Tasks.Domain;
using CloudGround.App.Modules.Tasks.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace CloudGround.App.Modules.Tasks.Features;

public sealed record CreateTaskRequest([Required, StringLength(TaskItem.TitleMaxLength)] string Title);

internal abstract record CreateTaskResult
{
    public sealed record Created(TaskResponse Task) : CreateTaskResult;

    public sealed record UnknownProject : CreateTaskResult;

    public sealed record ArchivedProject : CreateTaskResult;
}

internal sealed class CreateTaskHandler(TasksDbContext database, IProjectCatalog projects, TimeProvider clock)
{
    public async Task<CreateTaskResult> HandleAsync(Guid projectId, CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var project = await projects.FindAsync(projectId, cancellationToken);
        if (project is null)
        {
            return new CreateTaskResult.UnknownProject();
        }

        if (project.IsArchived)
        {
            return new CreateTaskResult.ArchivedProject();
        }

        var task = TaskItem.Create(projectId, request.Title, clock.GetUtcNow());
        database.Tasks.Add(task);
        await database.SaveChangesAsync(cancellationToken);
        return new CreateTaskResult.Created(task.ToResponse());
    }
}

internal static class CreateTaskEndpoint
{
    public static RouteGroupBuilder MapCreateTask(this RouteGroupBuilder group)
    {
        group.MapPost("/projects/{projectId:guid}/tasks", CreateAsync).ProducesValidationProblem();
        return group;
    }

    private static async Task<Results<CreatedAtRoute<TaskResponse>, NotFound, Conflict>> CreateAsync(
        Guid projectId,
        CreateTaskRequest request,
        CreateTaskHandler handler,
        CancellationToken cancellationToken) =>
        await handler.HandleAsync(projectId, request, cancellationToken) switch
        {
            CreateTaskResult.Created created => TypedResults.CreatedAtRoute(created.Task, GetTaskEndpoint.RouteName, new { id = created.Task.Id }),
            CreateTaskResult.UnknownProject => TypedResults.NotFound(),
            _ => TypedResults.Conflict(),
        };
}
