using System.Text.RegularExpressions;

namespace CloudGround.App.Architecture.Tests;

public sealed partial class GlobalUsingsTests
{
    private const string GlobalUsingsFile = "GlobalUsings.cs";

    private static readonly string[] SourceFolders = ["src", "tests"];

    private static readonly string[] SkippedFolders = ["bin", "obj", "node_modules", "Migrations"];

    private static readonly string[] MsBuildFilePatterns = ["*.csproj", "*.props", "*.targets"];

    private static readonly string GeneratedFolder = Path.Combine("Internal", "Generated");

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
            file => Assert.False(File.ReadAllText(file).Contains("<Using ", StringComparison.Ordinal), $"{file} has a Using item; it belongs in the {GlobalUsingsFile} of its project."));

    [Theory]
    [InlineData("using System.Net;")]
    [InlineData("global using System.Net;")]
    [InlineData("using static System.Math;")]
    [InlineData("global using static System.Math;")]
    [InlineData("using Json = System.Text.Json.JsonSerializer;")]
    [InlineData("    using System.Net;")]
    public void Using_directive_is_recognized(string line) =>
        Assert.Matches(UsingDirective(), line);

    [Theory]
    [InlineData("using var client = new HttpClient();")]
    [InlineData("    using var scope = host.Services.CreateScope();")]
    [InlineData("await using var app = builder.Build();")]
    [InlineData("using (response)")]
    [InlineData("using HttpClient client = new();")]
    public void Using_statement_is_not_a_directive(string line) =>
        Assert.DoesNotMatch(UsingDirective(), line);

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
        && !folder.EndsWith(GeneratedFolder, StringComparison.Ordinal);

    [GeneratedRegex(@"^\s*(global\s+)?using\s+(static\s+)?[\w.:]+\s*(;|=)")]
    private static partial Regex UsingDirective();
}
