var builder = DistributedApplication.CreateBuilder(args);

var database = builder.AddPostgres("postgres").WithDataVolume().AddDatabase("database");
var cache = builder.AddGarnet("cache");

var api = builder.AddProject<Projects.CloudGround_App_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(cache)
    .WaitFor(cache);

#if (sample)
var projectsMigrations = api
    .AddEFMigrations("projects-migrations", "CloudGround.App.Modules.Projects.Persistence.ProjectsDbContext")
    .WithMigrationsProject("../Modules/CloudGround.App.Modules.Projects/CloudGround.App.Modules.Projects.csproj")
    .WithReference(database)
    .WaitFor(database)
    .RunDatabaseUpdateOnStart();

api.WaitForCompletion(projectsMigrations);
#endif

builder.Build().Run();
