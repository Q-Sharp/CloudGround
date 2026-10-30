namespace CloudGround.App.Architecture.Tests;

public sealed partial class GlobalUsingsTests
{
    private const string GlobalUsingsFile = "GlobalUsings.cs";

    private static readonly string[] SourceFolders = ["src", "tests"];

    private static readonly string[] SkippedFolders = ["bin", "obj", "node_modules"];

    private static readonly string[] MsBuildFilePatterns = ["*.csproj", "*.props", "*.targets"];

    private static readonly string[] ToolWrittenFolders = [Path.Combine("Internal", "Generated"), Path.Combine("Persistence", "Migrations")];

    [Fact]
    public void Every_project_with_source_has_a_global_usings_file() =>
        Assert.All(
            Files("*.csproj").Select(project => Path.GetDirectoryName(project)!).Where(folder => Walk(folder, "*.cs").Any()),
            folder => Assert.True(File.Exists(Path.Combine(folder, GlobalUsingsFile)), $"{folder} has no {GlobalUsingsFile}."));

    [Fact]
    public void Only_the_global_usings_file_of_a_project_has_using_directives() =>
        Assert.All(
            Files("*.cs").Where(file => !IsGlobalUsingsFileOfAProject(file)),
            file => Assert.False(File.ReadLines(file).Any(UsingDirective().IsMatch), $"{file} has a using directive; it belongs in the {GlobalUsingsFile} of its project."));

    [Fact]
    public void No_project_file_has_a_using_item() =>
        Assert.All(
            MsBuildFiles(),
            file => Assert.False(UsingItem().IsMatch(File.ReadAllText(file)), $"{file} has a Using item; it belongs in the {GlobalUsingsFile} of its project."));

    [Theory]
    [InlineData("using System.Net;")]
    [InlineData("global using System.Net;")]
    [InlineData("using static System.Math;")]
    [InlineData("global using static System.Math;")]
    [InlineData("using static Foo<Bar>;")]
    [InlineData("using unsafe P = int*;")]
    [InlineData("global using unsafe P = int*;")]
    [InlineData("using Json = System.Text.Json.JsonSerializer;")]
    [InlineData("namespace X; using Y;")]
    [InlineData("    using System.Net;")]
    public void Using_directive_is_recognized(string line) =>
        Assert.Matches(UsingDirective(), line);

    [Theory]
    [InlineData("using var client = new HttpClient();")]
    [InlineData("    using var scope = host.Services.CreateScope();")]
    [InlineData("await using var app = builder.Build();")]
    [InlineData("using (response)")]
    [InlineData("using HttpClient client = new();")]
    [InlineData("namespace X;")]
    public void Using_statement_is_not_a_directive(string line) =>
        Assert.DoesNotMatch(UsingDirective(), line);

    [Theory]
    [InlineData("<Using Include=\"Xunit\" />")]
    [InlineData("<Using\n    Include=\"Xunit\" />")]
    public void Using_item_is_recognized(string element) =>
        Assert.Matches(UsingItem(), element);

    [Fact]
    public void Using_task_is_not_a_using_item() =>
        Assert.DoesNotMatch(UsingItem(), "<UsingTask TaskName=\"Build\" />");

    [Theory]
    [InlineData("src/Modules/Shop/Persistence/Migrations", false)]
    [InlineData("src/CloudGround.App.Api/Internal/Generated", false)]
    [InlineData("src/Modules/Shop/Migrations", true)]
    [InlineData("src/Modules/Shop/Seed/Migrations", true)]
    [InlineData("src/Modules/Shop/OldPersistence/Migrations", true)]
    public void Only_folders_a_tool_writes_are_skipped(string folder, bool walked) =>
        Assert.Equal(walked, IsWalked(folder.Replace('/', Path.DirectorySeparatorChar)));

    private static bool IsGlobalUsingsFileOfAProject(string file) =>
        Path.GetFileName(file) == GlobalUsingsFile
        && Directory.EnumerateFiles(Path.GetDirectoryName(file)!, "*.csproj").Any();

    private static IEnumerable<string> MsBuildFiles() =>
        MsBuildFilePatterns.SelectMany(pattern =>
            Directory.EnumerateFiles(ModuleCatalog.SolutionRoot(), pattern).Concat(Files(pattern)));

    private static IEnumerable<string> Files(string pattern) =>
        SourceFolders.SelectMany(folder => Walk(Path.Combine(ModuleCatalog.SolutionRoot(), folder), pattern));

    private static IEnumerable<string> Walk(string folder, string pattern) =>
        Directory.EnumerateFiles(folder, pattern)
            .Concat(Directory.EnumerateDirectories(folder).Where(IsWalked).SelectMany(child => Walk(child, pattern)));

    private static bool IsWalked(string folder) =>
        !SkippedFolders.Contains(Path.GetFileName(folder))
        && !ToolWrittenFolders.Any(written => folder.EndsWith(Path.DirectorySeparatorChar + written, StringComparison.Ordinal));

    [GeneratedRegex(@"^\s*(namespace\s+[\w.]+\s*[;{]\s*)?(global\s+)?using\s+(static\s|unsafe\s|[\w.:]+\s*[;=])")]
    private static partial Regex UsingDirective();

    [GeneratedRegex(@"<Using\s")]
    private static partial Regex UsingItem();
}
