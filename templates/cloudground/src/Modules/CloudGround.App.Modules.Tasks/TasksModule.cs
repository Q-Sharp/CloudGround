using CloudGround.App.Modules.Tasks.Features;
using CloudGround.App.Modules.Tasks.Messaging;
using CloudGround.App.Modules.Tasks.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Wolverine;

namespace CloudGround.App.Modules.Tasks;

public static class TasksModule
{
    public static IHostApplicationBuilder AddTasksModule(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddNpgsqlDbContext<TasksDbContext>("database", configureDbContextOptions: options =>
            options.UseNpgsql(npgsql => npgsql.MigrationsHistoryTable(HistoryRepository.DefaultTableName, TasksDbContext.Schema)));

        builder.Services.AddValidation();
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddScoped<CreateTaskHandler>();
        builder.Services.AddScoped<GetTaskHandler>();
        builder.Services.AddScoped<ListProjectTasksHandler>();
        builder.Services.AddScoped<CompleteTaskHandler>();
        builder.Services.AddScoped<ICloseProjectTasks, CloseProjectTasks>();
        builder.Services.ConfigureWolverine(options =>
        {
            options.Discovery.IncludeAssembly(typeof(TasksModule).Assembly);
            options.CodeGeneration.AlwaysUseServiceLocationFor<ICloseProjectTasks>();
        });

        return builder;
    }

    public static IEndpointRouteBuilder MapTasksModule(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("/api")
            .WithTags("Tasks")
            .MapCreateTask()
            .MapGetTask()
            .MapListProjectTasks()
            .MapCompleteTask();

        return endpoints;
    }
}
