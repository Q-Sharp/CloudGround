namespace CloudGround.Templates.Tests;

[Collection(SharedTemplatePackage.Name)]
public sealed class ModuleSkillTests(TemplatePackage package)
{
    [Theory]
    [InlineData("Skills.Shop", "false")]
    [InlineData("Skills.Sample", "true")]
    public async Task Scaffolded_module_follows_the_architecture_rules(string name, string sample)
    {
        var solution = await package.GenerateAsync(name, "--sample", sample);
        var architectureOutput = Path.Combine(solution.Root, "tests", $"{name}.Architecture.Tests", "bin", "Debug", "net10.0");

        await SkillScaffold.ApplyModuleAsync(solution, "OrderHistory");
        await SkillScaffold.AddContractsAsync(solution, "OrderHistory");

        await SkillScaffold.AssertSucceedsAsync(solution, "build");
        await SkillScaffold.AssertSucceedsAsync(solution, "format", "--verify-no-changes", "--no-restore");
        Assert.True(File.Exists(Path.Combine(architectureOutput, $"{name}.Modules.OrderHistory.dll")));
        Assert.True(File.Exists(Path.Combine(architectureOutput, $"{name}.Modules.OrderHistory.Contracts.dll")));
        await SkillScaffold.AssertSucceedsAsync(solution, "test", "--no-build", "--project", $"tests/{name}.Architecture.Tests");
        await SkillScaffold.AssertSucceedsAsync(solution, "test", "--no-build", "--project", $"tests/{name}.Modules.OrderHistory.Tests");
        await SkillScaffold.AssertSucceedsAsync(solution, "test", "--no-build", "--project", $"tests/{name}.Integration.Tests", "--filter-class", "*AppHostModelTests");
    }

    [Fact]
    public async Task Scaffolded_feature_passes_the_solution_tests()
    {
        var solution = await package.GenerateAsync("Feature.Shop", "--sample", "false");

        await SkillScaffold.ApplyModuleAsync(solution, "OrderHistory");
        await SkillScaffold.ApplyExampleFeatureAsync(solution, "OrderHistory");
        await SkillScaffold.AssertSucceedsAsync(solution, "build");
        await SkillScaffold.AssertSucceedsAsync(solution, "tool", "restore");
        var migration = await DotnetCli.RunWithEnvironmentAsync(
            solution.Root,
            new Dictionary<string, string> { ["ConnectionStrings__database"] = "Host=localhost" },
            "ef", "migrations", "add", "AddItems",
            "--project", "src/Modules/Feature.Shop.Modules.OrderHistory",
            "--startup-project", "src/Feature.Shop.Api",
            "--context", "OrderHistoryDbContext",
            "--output-dir", "Persistence/Migrations");
        Assert.True(migration.ExitCode == 0, migration.Output);

        await SkillScaffold.AssertSucceedsAsync(solution, "build");
        await SkillScaffold.AssertSucceedsAsync(solution, "format", "--verify-no-changes", "--no-restore");
        var results = Path.Combine(solution.Root, "artifacts", "test-results");
        await SkillScaffold.AssertSucceedsAsync(solution, [.. DotnetCli.TestArguments("Feature.Shop.slnx"), "--report-xunit-junit", "--results-directory", results, "--results-directory-layout", "per-module"]);

        var passed = JUnitReports.PassedTests(results);
        Assert.Contains("Feature.Shop.Integration.Tests.ItemsApiTests.Created_item_can_be_read_anonymously", passed);
        Assert.Contains("Feature.Shop.Modules.OrderHistory.Tests.ItemTests.Created_item_keeps_its_name_and_creation_time", passed);
    }
}
