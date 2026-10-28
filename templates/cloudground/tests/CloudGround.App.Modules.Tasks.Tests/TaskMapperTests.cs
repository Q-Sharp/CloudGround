using CloudGround.App.Modules.Tasks.Domain;
using CloudGround.App.Modules.Tasks.Features;

namespace CloudGround.App.Modules.Tasks.Tests;

public sealed class TaskMapperTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 28, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ToResponse_copies_every_member()
    {
        var projectId = Guid.CreateVersion7(Now);
        var task = TaskItem.Create(projectId, "Write the brief", Now);
        task.TryComplete(Now.AddHours(1));

        var response = task.ToResponse();

        Assert.Equal(new TaskResponse(task.Id, projectId, "Write the brief", Now, Now.AddHours(1), null), response);
    }
}
