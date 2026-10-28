using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace CloudGround.App.Architecture.Tests;

public sealed class DomainTests
{
    [Fact]
    public void Domain_does_not_depend_on_asp_net_core_or_ef_core() =>
        Types().That().ResideInNamespaceMatching(@"^.+\.Modules\.[^.]+\.Domain(\..+)?$")
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceOrBelow("Microsoft.AspNetCore")
            .AndShould().NotDependOnAnyTypesThat().ResideInNamespaceOrBelow("Microsoft.EntityFrameworkCore")
            .WithoutRequiringPositiveResults()
            .Check(ModuleCatalog.Architecture);
}
