namespace CloudGround.App.Architecture.Tests;

public sealed class ModuleDiscoveryTests
{
    [Fact]
    public void Module_discovery_finds_every_module_project() =>
        Assert.Equal(
            ModuleCatalog.ModuleProjectNames.Order(),
            ModuleCatalog.Modules.Select(module => module.GetName().Name!).Order());
}
