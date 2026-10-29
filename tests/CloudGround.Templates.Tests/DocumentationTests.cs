namespace CloudGround.Templates.Tests;

[Collection(SharedTemplatePackage.Name)]
public sealed class DocumentationTests(TemplatePackage package)
{
    private const int GeneratedDecisionRecords = 20;
    private const int RepositoryDecisionRecords = 7;

    private static readonly string[] RepositoryFoldersWithOwnChecks = ["templates/", "docs/superpowers/", "artifacts/", "generated/", ".superpowers/", ".omc/"];

    private static readonly string[] EvidenceOfAiAssistedDevelopment = ["](docs/superpowers/specs/", "](docs/superpowers/plans/", "](docs/adr/", "](AGENTS.md)"];

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    public async Task Documented_paths_exist_in_the_generated_solution(string sample)
    {
        var solution = await package.GenerateAsync("Contoso.Shop", "--sample", sample);

        Assert.Empty(MarkdownDocuments.BrokenReferences(solution.Root, MarkdownDocuments.Files(solution.Root)));
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    public async Task Decision_records_name_their_alternatives(string sample)
    {
        var solution = await package.GenerateAsync("Contoso.Shop", "--sample", sample);
        var adr = Path.Combine(solution.Root, "docs", "adr");

        Assert.True(Directory.Exists(adr), "docs/adr does not exist.");
        Assert.Empty(MarkdownDocuments.AdrViolations(adr));
        Assert.True(MarkdownDocuments.DecisionRecords(adr).Count >= GeneratedDecisionRecords);
    }

    [Fact]
    public async Task Readme_shows_the_c4_diagrams()
    {
        var solution = await package.GenerateAsync("Contoso.Shop");
        var readme = File.ReadAllText(Path.Combine(solution.Root, "README.md"));

        Assert.StartsWith("```mermaid", MarkdownDocuments.Section(readme, "### System context").TrimStart(), StringComparison.Ordinal);
        Assert.StartsWith("```mermaid", MarkdownDocuments.Section(readme, "### Containers").TrimStart(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    public async Task Module_map_names_every_module(string sample)
    {
        var solution = await package.GenerateAsync("Contoso.Shop", "--sample", sample);
        var moduleMap = MarkdownDocuments.Section(File.ReadAllText(Path.Combine(solution.Root, "README.md")), "### Module map");
        var modules = Directory.Exists(Path.Combine(solution.Root, "src", "Modules"))
            ? Directory.GetDirectories(Path.Combine(solution.Root, "src", "Modules"))
                .Select(Path.GetFileName)
                .Where(name => !name!.EndsWith(".Contracts", StringComparison.Ordinal))
                .Select(name => name!["Contoso.Shop.Modules.".Length..])
                .ToList()
            : [];

        Assert.All(modules, module => Assert.Contains(module, moduleMap, StringComparison.Ordinal));
        if (modules.Count == 0)
        {
            Assert.Contains(".claude/skills/add-module/SKILL.md", moduleMap, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Documented_paths_exist_in_the_repository()
    {
        var repository = RepositoryRoot.Find();

        Assert.Empty(MarkdownDocuments.BrokenReferences(repository, MarkdownDocuments.Files(repository, RepositoryFoldersWithOwnChecks)));
    }

    [Fact]
    public void Repository_decision_records_name_their_alternatives()
    {
        var adr = Path.Combine(RepositoryRoot.Find(), "docs", "adr");

        Assert.True(Directory.Exists(adr), "docs/adr does not exist.");
        Assert.Empty(MarkdownDocuments.AdrViolations(adr));
        Assert.True(MarkdownDocuments.DecisionRecords(adr).Count >= RepositoryDecisionRecords);
    }

    [Fact]
    public void Repository_readme_links_the_evidence_of_ai_assisted_development()
    {
        var section = MarkdownDocuments.Section(File.ReadAllText(Path.Combine(RepositoryRoot.Find(), "README.md")), "## How this was built with AI");

        Assert.All(EvidenceOfAiAssistedDevelopment, link => Assert.Contains(link, section, StringComparison.Ordinal));
    }
}
