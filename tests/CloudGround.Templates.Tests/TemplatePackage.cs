namespace CloudGround.Templates.Tests;

public sealed class TemplatePackage : IAsyncLifetime
{
    private const string PackagingProject = "CloudGround.Templates.csproj";

    private readonly string workspace = Directory.CreateTempSubdirectory("cloudground-tests-").FullName;

    private string? solutionsOwner;

    private string Hive => Path.Combine(workspace, "hive");

    private string Solutions => Path.Combine(workspace, "solutions");

    public string PackagePath { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        var packages = Path.Combine(workspace, "packages");
        var pack = await DotnetCli.RunAsync(RepositoryRoot.Find(), "pack", PackagingProject, "-o", packages);
        Assert.True(pack.ExitCode == 0, pack.Output);
        PackagePath = Directory.GetFiles(packages, "*.nupkg").Single();

        var install = await DotnetCli.RunAsync(workspace, "new", "install", PackagePath, "--debug:custom-hive", Hive);
        Assert.True(install.ExitCode == 0, install.Output);
    }

    public void RemoveSolutionsExceptOf(string? test)
    {
        if (test == solutionsOwner)
        {
            return;
        }

        solutionsOwner = test;
        if (Directory.Exists(Solutions))
        {
            Directory.Delete(Solutions, recursive: true);
        }
    }

    public async ValueTask<GeneratedSolution> GenerateAsync(string name, params string[] options)
    {
        RemoveSolutionsExceptOf(TestContext.Current.Test?.UniqueID);
        var root = Path.Combine(Solutions, Guid.NewGuid().ToString("N"), name);
        var generate = await DotnetCli.RunAsync(workspace, ["new", "cloudground", "-n", name, "-o", root, "--debug:custom-hive", Hive, .. options]);
        Assert.True(generate.ExitCode == 0, generate.Output);
        return new GeneratedSolution(root, name);
    }

    public ValueTask DisposeAsync()
    {
        Directory.Delete(workspace, recursive: true);
        return ValueTask.CompletedTask;
    }
}
