namespace CloudGround.Templates.Tests;

[Collection(SharedTemplatePackage.Name)]
public sealed class ModuleSkillTests(TemplatePackage package)
{
    [Fact]
    public async Task Scaffolded_module_follows_the_architecture_rules()
    {
        var solution = await package.GenerateAsync("Skills.Shop", "--sample", "false");

        await SkillScaffold.ApplyModuleAsync(solution, "OrderHistory");
        await SkillScaffold.AddContractsAsync(solution, "OrderHistory");

        await SkillScaffold.AssertSucceedsAsync(solution, "build");
        await SkillScaffold.AssertSucceedsAsync(solution, "format", "--verify-no-changes", "--no-restore");
        Assert.True(File.Exists(Path.Combine(solution.Root, "tests", "Skills.Shop.Architecture.Tests", "bin", "Debug", "net10.0", "Skills.Shop.Modules.OrderHistory.dll")));
        Assert.True(File.Exists(Path.Combine(solution.Root, "tests", "Skills.Shop.Architecture.Tests", "bin", "Debug", "net10.0", "Skills.Shop.Modules.OrderHistory.Contracts.dll")));
        await SkillScaffold.AssertSucceedsAsync(solution, "test", "--no-build", "--project", "tests/Skills.Shop.Architecture.Tests");
        await SkillScaffold.AssertSucceedsAsync(solution, "test", "--no-build", "--project", "tests/Skills.Shop.Modules.OrderHistory.Tests");
        await SkillScaffold.AssertSucceedsAsync(solution, "test", "--no-build", "--project", "tests/Skills.Shop.Integration.Tests", "--filter-class", "*AppHostModelTests");
    }
}
