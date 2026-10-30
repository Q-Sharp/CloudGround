namespace CloudGround.App.Modules.Projects.Tests;

public sealed class ProjectTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 28, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_sets_name_description_and_creation_time()
    {
        var project = Project.Create("Website relaunch", "New design", Now);

        Assert.Equal("Website relaunch", project.Name);
        Assert.Equal("New design", project.Description);
        Assert.Equal(Now, project.CreatedAt);
        Assert.Null(project.ArchivedAt);
    }

    [Fact]
    public void Create_assigns_a_version_7_id()
    {
        var project = Project.Create("Website relaunch", null, Now);

        Assert.Equal(7, project.Id.Version);
    }

    [Fact]
    public void TryArchive_sets_archived_time()
    {
        var project = Project.Create("Website relaunch", null, Now);

        Assert.True(project.TryArchive(Now.AddDays(1)));
        Assert.Equal(Now.AddDays(1), project.ArchivedAt);
    }

    [Fact]
    public void TryArchive_on_archived_project_returns_false_and_keeps_first_time()
    {
        var project = Project.Create("Website relaunch", null, Now);
        project.TryArchive(Now.AddDays(1));

        Assert.False(project.TryArchive(Now.AddDays(2)));
        Assert.Equal(Now.AddDays(1), project.ArchivedAt);
    }

    [Fact]
    public void Timestamps_are_kept_at_microsecond_precision()
    {
        var project = Project.Create("Website relaunch", null, Now.AddTicks(7));

        project.TryArchive(Now.AddDays(1).AddTicks(3));

        Assert.Equal(Now, project.CreatedAt);
        Assert.Equal(Now.AddDays(1), project.ArchivedAt);
    }
}
