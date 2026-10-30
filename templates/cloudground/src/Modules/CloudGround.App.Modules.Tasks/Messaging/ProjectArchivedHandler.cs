namespace CloudGround.App.Modules.Tasks.Messaging;

public static class ProjectArchivedHandler
{
    public static Task HandleAsync(ProjectArchived message, ICloseProjectTasks tasks, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(tasks);
        return tasks.CloseOpenTasksAsync(message.ProjectId, cancellationToken);
    }
}
