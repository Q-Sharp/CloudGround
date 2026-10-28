using Microsoft.EntityFrameworkCore;

namespace CloudGround.App.Architecture.Tests;

public sealed class PersistenceTests
{
    [Fact]
    public void Every_module_db_context_uses_its_own_schema()
    {
        foreach (var module in ModuleCatalog.Modules)
        {
            foreach (var contextType in module.GetTypes().Where(type => type.IsSubclassOf(typeof(DbContext)) && !type.IsAbstract))
            {
                using var context = CreateWithoutConnection(contextType);

                Assert.Equal(ModuleCatalog.NameOf(module), context.Model.GetDefaultSchema(), ignoreCase: true);
            }
        }
    }

    private static DbContext CreateWithoutConnection(Type contextType)
    {
        var options = (DbContextOptionsBuilder)Activator.CreateInstance(typeof(DbContextOptionsBuilder<>).MakeGenericType(contextType))!;
        return (DbContext)Activator.CreateInstance(contextType, options.UseNpgsql().Options)!;
    }
}
