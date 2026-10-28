using CloudGround.App.Modules.Tasks.Messaging;
using CloudGround.App.Modules.Tasks.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CloudGround.App.Modules.Tasks.Features;

internal sealed class CloseProjectTasks(TasksDbContext database, TimeProvider clock) : ICloseProjectTasks
{
    public async Task CloseOpenTasksAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var openTasks = await database.Tasks
            .Where(task => task.ProjectId == projectId && task.CompletedAt == null && task.ClosedAt == null)
            .ToListAsync(cancellationToken);

        var now = clock.GetUtcNow();
        openTasks.ForEach(task => task.Close(now));
        await database.SaveChangesAsync(cancellationToken);
    }
}
