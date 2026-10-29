using System.Text.RegularExpressions;

namespace CloudGround.Templates.Tests;

public static partial class SkillFrontMatter
{
    private const string Delimiter = "---";
    private const int MaximumDescriptionLength = 1024;

    public static (string Name, string Description) Read(string skillFile)
    {
        var lines = File.ReadAllLines(skillFile);
        Assert.True(lines.Length > 0 && lines[0] == Delimiter, $"{skillFile} does not start with front matter.");

        var fields = lines.Skip(1)
            .TakeWhile(line => line != Delimiter)
            .Select(line => line.Split(':', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim(), StringComparer.Ordinal);

        return (fields.GetValueOrDefault("name", string.Empty), fields.GetValueOrDefault("description", string.Empty));
    }

    public static IReadOnlyList<string> SkillFiles(string root)
    {
        var skills = Path.Combine(root, ".claude", "skills");
        return Directory.Exists(skills)
            ? Directory.GetDirectories(skills).Select(skill => Path.Combine(skill, "SKILL.md")).ToList()
            : [];
    }

    public static void AssertSkillsAreNamedAfterTheirFolder(string root)
    {
        var skills = SkillFiles(root);

        Assert.NotEmpty(skills);
        Assert.All(skills, skill =>
        {
            var (name, description) = Read(skill);
            Assert.Equal(Path.GetFileName(Path.GetDirectoryName(skill)), name);
            Assert.Matches(SkillName(), name);
            Assert.InRange(description.Length, 1, MaximumDescriptionLength);
        });
    }

    [GeneratedRegex("^[a-z0-9-]{1,64}$")]
    private static partial Regex SkillName();
}
