using System.Text.RegularExpressions;

namespace CloudGround.Templates.Tests;

public sealed partial class GlobalUsingsTests
{
    private const string GlobalUsingsFile = "GlobalUsings.cs";

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
    public void Test_project_file_has_no_using_item() =>
        Assert.DoesNotContain("<Using ", File.ReadAllText(Path.Combine(ProjectFolder, "CloudGround.Templates.Tests.csproj")), StringComparison.Ordinal);

    [Theory]
    [InlineData("using System.Net;")]
    [InlineData("global using System.Net;")]
    [InlineData("using static System.Math;")]
    [InlineData("using Json = System.Text.Json.JsonSerializer;")]
    public void Using_directive_is_recognized(string line) =>
        Assert.Matches(UsingDirective(), line);

    [Theory]
    [InlineData("using var client = new HttpClient();")]
    [InlineData("await using var app = builder.Build();")]
    [InlineData("using (response)")]
    [InlineData("using HttpClient client = new();")]
    public void Using_statement_is_not_a_directive(string line) =>
        Assert.DoesNotMatch(UsingDirective(), line);

    private static IEnumerable<string> SourceFiles() =>
        Directory.EnumerateFiles(ProjectFolder, "*.cs", SearchOption.AllDirectories)
            .Where(file => Path.GetRelativePath(ProjectFolder, file).Split(Path.DirectorySeparatorChar) is not ["bin" or "obj", ..]);

    [GeneratedRegex(@"^\s*(global\s+)?using\s+(static\s+)?[\w.:]+\s*(;|=)")]
    private static partial Regex UsingDirective();
}
