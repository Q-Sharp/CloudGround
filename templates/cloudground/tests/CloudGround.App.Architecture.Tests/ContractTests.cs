namespace CloudGround.App.Architecture.Tests;

public sealed class ContractTests
{
    private const string ContractsProjectSuffix = ".Contracts";

    private static readonly string[] FrameworkNamespaces = ["Microsoft.AspNetCore", "Microsoft.EntityFrameworkCore", "Wolverine"];

    [Fact]
    public void Modules_reference_other_modules_only_through_contracts()
    {
        foreach (var project in ModuleCatalog.ModuleProjectFiles)
        {
            var references = XDocument.Load(project).Descendants("ProjectReference")
                .Select(reference => Path.GetFileNameWithoutExtension((string)reference.Attribute("Include")!));

            Assert.All(references, reference =>
                Assert.True(reference.EndsWith(ContractsProjectSuffix, StringComparison.Ordinal), $"{Path.GetFileName(project)} references {reference}"));
        }
    }

    [Fact]
    public void Contracts_do_not_depend_on_asp_net_core_ef_core_or_wolverine()
    {
        foreach (var contracts in ModuleCatalog.Contracts)
        {
            foreach (var frameworkNamespace in FrameworkNamespaces)
            {
                Types().That().ResideInAssembly(contracts)
                    .Should().NotDependOnAnyTypesThat().ResideInNamespaceOrBelow(frameworkNamespace)
                    .WithoutRequiringPositiveResults()
                    .Check(ModuleCatalog.Architecture);
            }
        }
    }

    [Fact]
    public void Contracts_do_not_depend_on_any_module()
    {
        foreach (var contracts in ModuleCatalog.Contracts)
        {
            foreach (var module in ModuleCatalog.Modules)
            {
                Types().That().ResideInAssembly(contracts)
                    .Should().NotDependOnAny(Types().That().ResideInAssembly(module))
                    .WithoutRequiringPositiveResults()
                    .Check(ModuleCatalog.Architecture);
            }
        }
    }
}
