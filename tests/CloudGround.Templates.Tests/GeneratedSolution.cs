namespace CloudGround.Templates.Tests;

public sealed record GeneratedSolution(string Root, string Name)
{
    private static readonly EnumerationOptions AllFilesIncludingHidden = new()
    {
        RecurseSubdirectories = true,
        AttributesToSkip = FileAttributes.None,
    };

    public IReadOnlyList<string> RelativeFiles() =>
        Directory.EnumerateFiles(Root, "*", AllFilesIncludingHidden)
            .Select(path => Path.GetRelativePath(Root, path).Replace('\\', '/'))
            .ToList();
}
