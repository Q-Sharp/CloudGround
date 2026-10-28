namespace CloudGround.App.Modules.Tasks.Messaging;

public interface ICloseProjectTasks
{
    Task CloseOpenTasksAsync(Guid projectId, CancellationToken cancellationToken);
}
