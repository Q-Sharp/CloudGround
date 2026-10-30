namespace CloudGround.App.Modules.ModuleName;

public static class ModuleNameModule
{
    public static IHostApplicationBuilder AddModuleNameModule(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddNpgsqlDbContext<ModuleNameDbContext>("database", configureDbContextOptions: options =>
            options.UseNpgsql(npgsql => npgsql.MigrationsHistoryTable(HistoryRepository.DefaultTableName, ModuleNameDbContext.Schema)));

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(ModuleNamePolicies.Edit, policy => policy.RequireRole(ModuleNamePolicies.EditorRole));
        builder.Services.AddValidation();
        builder.Services.TryAddSingleton(TimeProvider.System);

        return builder;
    }

    public static IEndpointRouteBuilder MapModuleNameModule(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("/api/modulename")
            .WithTags("ModuleName");

        return endpoints;
    }
}
