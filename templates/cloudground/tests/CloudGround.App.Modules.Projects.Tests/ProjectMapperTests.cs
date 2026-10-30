namespace CloudGround.App.Modules.Projects.Tests;

public sealed class ProjectMapperTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 28, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ToResponse_copies_every_member()
    {
        var project = Project.Create("Website relaunch", "New design", Now);
        project.TryArchive(Now.AddDays(1));

        var response = project.ToResponse();

        Assert.Equal(new ProjectResponse(project.Id, "Website relaunch", "New design", Now, Now.AddDays(1)), response);
    }
}
