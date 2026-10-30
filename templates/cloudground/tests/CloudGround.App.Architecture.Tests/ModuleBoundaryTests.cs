namespace CloudGround.App.Architecture.Tests;

public sealed class ModuleBoundaryTests
{
    [Fact]
    public void Modules_do_not_depend_on_each_others_internals()
    {
        foreach (var module in ModuleCatalog.Modules)
        {
            foreach (var other in ModuleCatalog.Modules.Where(other => other != module))
            {
                Types().That().ResideInAssembly(module)
                    .Should().NotDependOnAny(Types().That().ResideInAssembly(other))
                    .Check(ModuleCatalog.Architecture);
            }
        }
    }

    [Fact]
    public void Modules_expose_only_their_module_class_validated_requests_and_message_handlers()
    {
        foreach (var module in ModuleCatalog.Modules)
        {
            var unexpected = module.GetExportedTypes()
                .Where(type => type.Name != $"{ModuleCatalog.NameOf(module)}Module")
                .Where(type => !type.Name.EndsWith("Request", StringComparison.Ordinal))
                .Where(type => !IsMessageHandlerSurface(module, type))
                .Where(type => !type.IsSubclassOf(typeof(Migration)));

            Assert.Empty(unexpected);
        }
    }

    private static bool IsMessageHandlerSurface(Assembly module, Type type) =>
        type.Namespace == $"{module.GetName().Name}.Messaging"
        && (type.IsInterface || (type.IsAbstract && type.IsSealed && type.Name.EndsWith("Handler", StringComparison.Ordinal)));
}
