namespace CloudGround.Templates.Tests;

public static class JUnitReports
{
    private static readonly string[] UnsuccessfulOutcomes = ["failure", "error", "skipped"];

    public static IReadOnlyList<string> PassedTests(string directory) =>
        Directory.EnumerateFiles(directory, "*.junit.xml", SearchOption.AllDirectories)
            .SelectMany(file => XDocument.Load(file).Descendants("testcase"))
            .Where(testCase => !testCase.Elements().Any(element => UnsuccessfulOutcomes.Contains(element.Name.LocalName)))
            .Select(testCase => (string)testCase.Attribute("name")!)
            .ToList();
}
