namespace CloudGround.Templates.Tests;

public static partial class MarkdownDocuments
{
    private const string SupersededStatus = "Status: Superseded by ";

    private static readonly string[] CheckedRoots = ["src/", "tests/", "docs/", "templates/", ".github/", ".claude/", ".devcontainer/", ".config/"];

    private static readonly string[] UncheckedFragments = ["<", "*", "/bin/", "/obj/"];

    private static readonly string[] FoldersCreatedOnDemand = ["src/Modules", "src/Modules/"];

    private static readonly string[] ExternalSchemes = ["http://", "https://", "mailto:"];

    private static readonly string[] AdrSections = ["## Context", "## Decision", "## Alternatives", "## Consequences"];

    public static IEnumerable<string> Files(string root, params string[] excludedPrefixes) =>
        Directory.EnumerateFiles(root, "*.md", new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = FileAttributes.None })
            .Select(file => Path.GetRelativePath(root, file).Replace('\\', '/'))
            .Where(file => file != "CHANGELOG.md")
            .Where(file => !file.Contains("node_modules/", StringComparison.Ordinal))
            .Where(file => !file.Contains("/bin/", StringComparison.Ordinal) && !file.Contains("/obj/", StringComparison.Ordinal))
            .Where(file => !excludedPrefixes.Any(prefix => file.StartsWith(prefix, StringComparison.Ordinal)))
            .Select(file => Path.Combine(root, file));

    public static IReadOnlyList<string> BrokenReferences(string root, IEnumerable<string> markdownFiles)
    {
        var broken = new List<string>();
        foreach (var file in markdownFiles)
        {
            var text = FencedBlock().Replace(File.ReadAllText(file), string.Empty);
            var name = Path.GetRelativePath(root, file);

            broken.AddRange(Link().Matches(text)
                .Select(match => match.Groups["target"].Value)
                .Where(target => !ExternalSchemes.Any(scheme => target.StartsWith(scheme, StringComparison.Ordinal)))
                .Where(target => !Exists(Path.Combine(Path.GetDirectoryName(file)!, Uri.UnescapeDataString(target))))
                .Select(target => $"{name}: {target}"));

            if (IsSupersededDecisionRecord(text))
            {
                continue;
            }

            broken.AddRange(CodeSpan().Matches(text)
                .Select(match => match.Groups["path"].Value)
                .Where(path => CheckedRoots.Any(checkedRoot => path.StartsWith(checkedRoot, StringComparison.Ordinal)))
                .Where(path => !UncheckedFragments.Any(fragment => path.Contains(fragment, StringComparison.Ordinal)))
                .Where(path => !FoldersCreatedOnDemand.Contains(path))
                .Where(path => !Exists(Path.Combine(root, path)))
                .Select(path => $"{name}: `{path}`"));
        }

        return broken;
    }

    public static IReadOnlyList<string> AdrViolations(string adrDirectory)
    {
        var violations = new List<string>();
        var index = File.ReadAllText(Path.Combine(adrDirectory, "README.md"));

        foreach (var adr in DecisionRecords(adrDirectory))
        {
            var name = Path.GetFileName(adr);
            var lines = File.ReadAllLines(adr);

            if (!lines[0].StartsWith($"# {name[..4]}. ", StringComparison.Ordinal))
            {
                violations.Add($"{name}: the title does not start with `# {name[..4]}. `");
            }

            if (!lines.Any(line => line.StartsWith("Status: ", StringComparison.Ordinal)))
            {
                violations.Add($"{name}: no status line");
            }

            var sections = AdrSections.Select(section => Array.IndexOf(lines, section)).ToList();
            if (sections.Any(position => position < 0) || !sections.SequenceEqual(sections.Order()))
            {
                violations.Add($"{name}: the sections are not {string.Join(", ", AdrSections)} in this order");
            }
            else if (!lines[sections[2]..sections[3]].Any(line => line.StartsWith("- ", StringComparison.Ordinal)))
            {
                violations.Add($"{name}: no alternative");
            }

            if (!index.Contains($"({name})", StringComparison.Ordinal))
            {
                violations.Add($"{name}: missing from README.md");
            }
        }

        return violations;
    }

    public static IReadOnlyList<string> DecisionRecords(string adrDirectory) =>
        Directory.EnumerateFiles(adrDirectory, "*.md")
            .Where(file => DecisionRecordName().IsMatch(Path.GetFileName(file)))
            .Order(StringComparer.Ordinal)
            .ToList();

    public static string Section(string markdown, string heading)
    {
        var lines = markdown.Split('\n');
        var start = Array.FindIndex(lines, line => line.TrimEnd() == heading);
        Assert.True(start >= 0, $"No heading `{heading}`.");
        var level = heading[..(heading.IndexOf(' ', StringComparison.Ordinal) + 1)];

        return string.Join('\n', lines.Skip(start + 1).TakeWhile(line => !IsHeadingAtOrAbove(line, level)));
    }

    private static bool IsHeadingAtOrAbove(string line, string level) =>
        line.StartsWith('#') && line.IndexOf(' ', StringComparison.Ordinal) is var end && end > 0 && end + 1 <= level.Length && line[..end].All(character => character == '#');

    private static bool Exists(string path) => File.Exists(path) || Directory.Exists(path);

    private static bool IsSupersededDecisionRecord(string markdown) =>
        markdown.Split('\n').Any(line => line.StartsWith(SupersededStatus, StringComparison.Ordinal));

    [GeneratedRegex(@"^[ \t]*```.*?^[ \t]*```[^\n]*$", RegexOptions.Multiline | RegexOptions.Singleline)]
    private static partial Regex FencedBlock();

    [GeneratedRegex(@"\]\((?<target>[^)\s#]+)(?:#[^)\s]*)?\)")]
    private static partial Regex Link();

    [GeneratedRegex(@"`(?<path>[^`\s]+)`")]
    private static partial Regex CodeSpan();

    [GeneratedRegex(@"^\d{4}-[a-z0-9-]+\.md$")]
    private static partial Regex DecisionRecordName();
}
