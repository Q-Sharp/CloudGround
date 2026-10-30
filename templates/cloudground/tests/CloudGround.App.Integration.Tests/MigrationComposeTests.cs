namespace CloudGround.App.Integration.Tests;

[Collection(PublishedDeployment.Name)]
public sealed class MigrationComposeTests(PublishedCompose compose)
{
    [Theory]
    [InlineData("projects-migrations")]
    [InlineData("tasks-migrations")]
    public void Migrations_run_once_before_the_api(string migrations)
    {
        Assert.NotEmpty(compose.Service(migrations));
        Assert.DoesNotContain("restart:", compose.Service(migrations), StringComparison.Ordinal);
        Assert.Matches(new Regex($@"{migrations}:\s+condition: ""service_completed_successfully"""), compose.Service("api"));
        Assert.Matches(new Regex(@"postgres:\s+condition: ""service_healthy"""), compose.Service(migrations));
    }

    [Theory]
    [InlineData("projects-migrations")]
    [InlineData("tasks-migrations")]
    public void Migration_bundles_run_on_the_ubuntu_aspnet_image(string migrations)
    {
        var dockerfile = File.ReadAllLines(Path.Combine(compose.OutputDirectory, $"{migrations}.Dockerfile"));

        Assert.Equal("FROM mcr.microsoft.com/dotnet/aspnet:10.0-resolute", dockerfile.First(line => line.StartsWith("FROM ", StringComparison.Ordinal)));
    }

    [Fact]
    public void Migrations_run_one_after_another()
    {
        Assert.Matches(new Regex(@"projects-migrations:\s+condition: ""service_completed_successfully"""), compose.Service("tasks-migrations"));
    }
}
