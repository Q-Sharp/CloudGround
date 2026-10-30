namespace CloudGround.App.Modules.Projects;

public static class ProjectsModule
{
    public static IHostApplicationBuilder AddProjectsModule(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddNpgsqlDbContext<ProjectsDbContext>("database", configureDbContextOptions: options =>
            options.UseNpgsql(npgsql => npgsql.MigrationsHistoryTable(HistoryRepository.DefaultTableName, ProjectsDbContext.Schema)));

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(ProjectsPolicies.Edit, policy => policy.RequireRole(ProjectsPolicies.EditorRole));
        builder.Services.AddValidation();
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddScoped<CreateProjectHandler>();
        builder.Services.AddScoped<GetProjectHandler>();
        builder.Services.AddScoped<ListProjectsHandler>();
        builder.Services.AddScoped<ArchiveProjectHandler>();
        builder.Services.AddScoped<IProjectCatalog, ProjectCatalog>();

        return builder;
    }

    public static IEndpointRouteBuilder MapProjectsModule(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("/api/projects")
            .WithTags("Projects")
            .MapCreateProject()
            .MapGetProject()
            .MapListProjects()
            .MapArchiveProject();

        return endpoints;
    }
}
