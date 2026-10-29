using Aspire.Hosting.EntityFrameworkCore;

namespace CloudGround.App.AppHost;

internal static class ModuleMigrations
{
    private const string ModuleNamespace = "CloudGround.App.Modules";
    private const string BundleBaseImage = "mcr.microsoft.com/dotnet/aspnet:10.0-resolute";

    public static IResourceBuilder<ProjectResource> WithModuleMigrations(
        this IResourceBuilder<ProjectResource> api,
        string module,
        IResourceBuilder<PostgresDatabaseResource> database)
    {
        var previous = api.ApplicationBuilder.Resources.OfType<EFMigrationResource>().LastOrDefault();
        var project = $"{ModuleNamespace}.{module}";

        var migrations = api
            .AddEFMigrations($"{module.ToLowerInvariant()}-migrations", $"{project}.Persistence.{module}DbContext")
            .WithMigrationsProject($"../Modules/{project}/{project}.csproj")
            .WithReference(database)
            .WaitFor(database)
            .RunDatabaseUpdateOnStart()
            .PublishAsMigrationBundle(publishContainer: true, baseImage: BundleBaseImage);

        if (previous is not null)
        {
            migrations.WaitForCompletion(api.ApplicationBuilder.CreateResourceBuilder(previous));
        }

        return api.WaitForCompletion(migrations);
    }
}
