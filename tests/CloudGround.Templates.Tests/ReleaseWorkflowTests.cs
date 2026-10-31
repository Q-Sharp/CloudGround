namespace CloudGround.Templates.Tests;

public sealed class ReleaseWorkflowTests
{
    private const string PublishJob = "  publish:";

    [Fact]
    public void Only_the_publish_job_can_request_an_identity_token()
    {
        var lines = WorkflowLines();

        Assert.Contains("      id-token: write", lines.SkipWhile(line => line != PublishJob));
        Assert.DoesNotContain(lines.TakeWhile(line => line != PublishJob), line => line.Contains("id-token", StringComparison.Ordinal));
    }

    [Fact]
    public void Publish_job_logs_in_with_the_nuget_profile_name()
    {
        var lines = WorkflowLines().SkipWhile(line => line != PublishJob).ToList();

        Assert.Contains("        uses: NuGet/login@v1", lines);
        Assert.Contains("          user: ${{ secrets.NUGET_USER }}", lines);
    }

    [Fact]
    public void Package_is_pushed_with_the_short_lived_key_of_the_login()
    {
        var lines = WorkflowLines();

        Assert.Contains("          NUGET_API_KEY: ${{ steps.login.outputs.NUGET_API_KEY }}", lines);
        Assert.DoesNotContain(lines, line => line.Contains("secrets.NUGET_API_KEY", StringComparison.Ordinal));
    }

    private static string[] WorkflowLines() =>
        File.ReadAllLines(Path.Combine(RepositoryRoot.Find(), ".github", "workflows", "release-please.yml"));
}
