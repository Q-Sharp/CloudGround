using System.Reflection;
using ArchUnitNET.Loader;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;

namespace CloudGround.App.Architecture.Tests;

internal static class ModuleCatalog
{
    private const string ApiAssemblyName = "CloudGround.App.Api";
    private const string ModulePrefix = "CloudGround.App.Modules.";
    private const string ContractsSuffix = ".Contracts";

    public static IReadOnlyList<Assembly> Modules { get; } = DiscoverAssemblies(contracts: false);

    public static IReadOnlyList<Assembly> Contracts { get; } = DiscoverAssemblies(contracts: true);

    public static IReadOnlyList<Assembly> HostAndModules { get; } = [Assembly.Load(ApiAssemblyName), .. Modules];

    public static IReadOnlyList<string> ModuleProjectNames { get; } = FindModuleProjectNames();

    public static IReadOnlyList<string> ModuleProjectFiles { get; } =
        [.. ModuleProjectNames.Select(name => Path.Combine(SolutionRoot(), "src", "Modules", name, $"{name}.csproj"))];

    public static ArchitectureModel Architecture { get; } = new ArchLoader().LoadAssemblies([.. HostAndModules, .. Contracts]).Build();

    public static string NameOf(Assembly module) => module.GetName().Name![ModulePrefix.Length..];

    private static List<Assembly> DiscoverAssemblies(bool contracts) =>
        Directory.EnumerateFiles(AppContext.BaseDirectory, $"{ModulePrefix}*.dll")
            .Select(file => Path.GetFileNameWithoutExtension(file))
            .Where(name => name.EndsWith(ContractsSuffix, StringComparison.Ordinal) == contracts)
            .Select(name => Assembly.Load(name))
            .ToList();

    private static List<string> FindModuleProjectNames()
    {
        var modules = new DirectoryInfo(Path.Combine(SolutionRoot(), "src", "Modules"));
        return modules.Exists
            ? modules.EnumerateDirectories()
                .Select(directory => directory.Name)
                .Where(name => !name.EndsWith(ContractsSuffix, StringComparison.Ordinal))
                .ToList()
            : [];
    }

    public static string SolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !directory.EnumerateFiles("*.slnx").Any())
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("No solution file above the test output.");
    }
}
