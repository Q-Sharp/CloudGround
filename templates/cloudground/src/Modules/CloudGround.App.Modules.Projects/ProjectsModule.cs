using CloudGround.App.Modules.Projects.Features;
using CloudGround.App.Modules.Projects.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace CloudGround.App.Modules.Projects;

public static class ProjectsModule
{
    public static IHostApplicationBuilder AddProjectsModule(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddNpgsqlDbContext<ProjectsDbContext>("database", configureDbContextOptions: options =>
            options.UseNpgsql(npgsql => npgsql.MigrationsHistoryTable(HistoryRepository.DefaultTableName, ProjectsDbContext.Schema)));

        builder.Services.AddValidation();
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddScoped<CreateProjectHandler>();
        builder.Services.AddScoped<GetProjectHandler>();
        builder.Services.AddScoped<ListProjectsHandler>();
        builder.Services.AddScoped<ArchiveProjectHandler>();

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
