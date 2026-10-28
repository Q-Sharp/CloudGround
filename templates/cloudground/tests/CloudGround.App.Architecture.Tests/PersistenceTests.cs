using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CloudGround.App.Architecture.Tests;

public sealed class PersistenceTests
{
    [Fact]
    public void Every_module_db_context_uses_its_own_schema()
    {
        foreach (var module in ModuleCatalog.Modules)
        {
            foreach (var contextType in DbContextTypes(module))
            {
                using var context = CreateWithoutConnection(contextType);

                Assert.Equal(ModuleCatalog.NameOf(module), context.Model.GetDefaultSchema(), ignoreCase: true);
            }
        }
    }

    [Fact]
    public void Every_module_db_context_retries_transient_failures()
    {
        foreach (var module in ModuleCatalog.Modules)
        {
            using var host = BuildHostWith(module);
            using var scope = host.Services.CreateScope();

            foreach (var contextType in DbContextTypes(module))
            {
                var context = (DbContext)scope.ServiceProvider.GetRequiredService(contextType);

                Assert.True(context.Database.CreateExecutionStrategy().RetriesOnFailure, contextType.FullName);
            }
        }
    }

    private static IEnumerable<Type> DbContextTypes(Assembly module) =>
        module.GetTypes().Where(type => type.IsSubclassOf(typeof(DbContext)) && !type.IsAbstract);

    private static DbContext CreateWithoutConnection(Type contextType)
    {
        var options = (DbContextOptionsBuilder)Activator.CreateInstance(typeof(DbContextOptionsBuilder<>).MakeGenericType(contextType))!;
        return (DbContext)Activator.CreateInstance(contextType, options.UseNpgsql().Options)!;
    }

    private static IHost BuildHostWith(Assembly module)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["ConnectionStrings:database"] = "Host=localhost";

        var moduleName = ModuleCatalog.NameOf(module);
        var addModule = module.GetType($"{module.GetName().Name}.{moduleName}Module", throwOnError: true)!.GetMethod($"Add{moduleName}Module")!;
        addModule.Invoke(null, [builder]);

        return builder.Build();
    }
}
