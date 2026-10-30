namespace CloudGround.Templates.Tests;

public sealed partial class GlobalUsingsTests
{
    private const string GlobalUsingsFile = "GlobalUsings.cs";

    private static readonly string[] MsBuildFilePatterns = ["*.csproj", "*.props", "*.targets"];

    private static readonly string ProjectFolder = Path.Combine(RepositoryRoot.Find(), "tests", "CloudGround.Templates.Tests");

    [Fact]
    public void Test_project_has_a_global_usings_file() =>
        Assert.True(File.Exists(Path.Combine(ProjectFolder, GlobalUsingsFile)));

    [Fact]
    public void Only_the_global_usings_file_has_using_directives() =>
        Assert.All(
            SourceFiles().Where(file => Path.GetRelativePath(ProjectFolder, file) != GlobalUsingsFile),
            file => Assert.False(File.ReadLines(file).Any(UsingDirective().IsMatch), $"{file} has a using directive; it belongs in {GlobalUsingsFile}."));

    [Fact]
    public void No_project_file_has_a_using_item() =>
        Assert.All(
            MsBuildFiles(),
            file => Assert.False(UsingItem().IsMatch(File.ReadAllText(file)), $"{file} has a Using item; it belongs in {GlobalUsingsFile}."));

    [Theory]
    [InlineData("using System.Net;")]
    [InlineData("global using System.Net;")]
    [InlineData("using static System.Math;")]
    [InlineData("using static Foo<Bar>;")]
    [InlineData("using unsafe P = int*;")]
    [InlineData("global using unsafe P = int*;")]
    [InlineData("using Json = System.Text.Json.JsonSerializer;")]
    [InlineData("namespace X; using Y;")]
    public void Using_directive_is_recognized(string line) =>
        Assert.Matches(UsingDirective(), line);

    [Theory]
    [InlineData("using var client = new HttpClient();")]
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

    private static IEnumerable<string> SourceFiles() =>
        Directory.EnumerateFiles(ProjectFolder, "*.cs", SearchOption.AllDirectories)
            .Where(file => Path.GetRelativePath(ProjectFolder, file).Split(Path.DirectorySeparatorChar) is not ["bin" or "obj", ..]);

    private static IEnumerable<string> MsBuildFiles() =>
        MsBuildFilePatterns.SelectMany(pattern =>
            Directory.EnumerateFiles(ProjectFolder, pattern).Concat(Directory.EnumerateFiles(RepositoryRoot.Find(), pattern)));

    [GeneratedRegex(@"^\s*(namespace\s+[\w.]+\s*[;{]\s*)?(global\s+)?using\s+(static\s|unsafe\s|[\w.:]+\s*[;=])")]
    private static partial Regex UsingDirective();

    [GeneratedRegex(@"<Using\s")]
    private static partial Regex UsingItem();
}
