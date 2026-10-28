namespace CloudGround.Templates.Tests;

[Collection(SharedTemplatePackage.Name)]
public sealed class MappingGuardrailTests(TemplatePackage package)
{
    [Fact]
    public async Task Unmapped_response_member_fails_the_build()
    {
        var solution = await package.GenerateAsync("Guard.Rails");
        var response = Path.Combine(solution.Root, "src", "Modules", "Guard.Rails.Modules.Projects", "Features", "ProjectResponse.cs");
        var cancellation = TestContext.Current.CancellationToken;
        var withUnmappedMember = (await File.ReadAllTextAsync(response, cancellation))
            .Replace("DateTimeOffset? ArchivedAt)", "DateTimeOffset? ArchivedAt, string Owner)", StringComparison.Ordinal);
        await File.WriteAllTextAsync(response, withUnmappedMember, cancellation);

        var build = await DotnetCli.RunAsync(solution.Root, "build");

        Assert.NotEqual(0, build.ExitCode);
        Assert.Contains("RMG", build.Output, StringComparison.Ordinal);
    }
}
