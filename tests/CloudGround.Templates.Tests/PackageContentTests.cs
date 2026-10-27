using System.IO.Compression;

namespace CloudGround.Templates.Tests;

[Collection(SharedTemplatePackage.Name)]
public sealed class PackageContentTests(TemplatePackage package)
{
    [Fact]
    public void Package_contains_template_dotfiles()
    {
        var entries = ReadEntries(package.PackagePath);

        Assert.Contains(entries, entry => entry.EndsWith("cloudground/.gitignore", StringComparison.Ordinal));
        Assert.Contains(entries, entry => entry.EndsWith("cloudground/.editorconfig", StringComparison.Ordinal));
        Assert.Contains(entries, entry => entry.EndsWith("cloudground/.template.config/template.json", StringComparison.Ordinal));
        Assert.Contains(entries, entry => entry.EndsWith("cloudground/.github/workflows/ci.yml", StringComparison.Ordinal));
    }

    [Fact]
    public void Package_contains_no_build_output()
    {
        var entries = ReadEntries(package.PackagePath);

        Assert.DoesNotContain(entries, IsBuildOutput);
    }

    [Fact]
    public void Package_contains_readme()
    {
        var entries = ReadEntries(package.PackagePath);

        Assert.Contains("README.md", entries);
    }

    private static bool IsBuildOutput(string entry) =>
        entry.Contains("/bin/", StringComparison.Ordinal)
        || entry.Contains("/obj/", StringComparison.Ordinal)
        || entry.Contains("/.vs/", StringComparison.Ordinal)
        || entry.EndsWith(".user", StringComparison.Ordinal);

    private static List<string> ReadEntries(string packagePath)
    {
        using var archive = ZipFile.OpenRead(packagePath);
        return archive.Entries.Select(entry => entry.FullName).ToList();
    }
}
