using System.Text.RegularExpressions;

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
    }
}
