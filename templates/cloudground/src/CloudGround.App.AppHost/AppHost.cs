var builder = DistributedApplication.CreateBuilder(args);

var database = builder.AddPostgres("postgres").WithDataVolume().AddDatabase("database");
var cache = builder.AddGarnet("cache");
var messaging = builder.AddRabbitMQ("messaging").WithManagementPlugin();
var bffClientSecret = builder.AddParameter("bff-client-secret", new GenerateParameterDefault { MinLength = 32, Special = false }, secret: true, persist: true);
#pragma warning disable ASPIRECERTIFICATES001
var keycloak = builder.AddKeycloak("keycloak", port: 8380)
    .WithDataVolume()
    .WithoutHttpsCertificate()
    .WithRealmImport("./Realms")
    .WithEnvironment("BFF_CLIENT_SECRET", bffClientSecret);
#pragma warning restore ASPIRECERTIFICATES001

var api = builder.AddProject<Projects.CloudGround_App_Api>("api")
    .WithHttpHealthCheck("/health", endpointName: "http")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithReference(keycloak)
    .WaitFor(keycloak);

builder.AddProject<Projects.CloudGround_App_Bff>("bff")
    .WithHttpHealthCheck("/health", endpointName: "http")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithEnvironment("Bff__ClientSecret", bffClientSecret);

#if (sample)
var projectsMigrations = api
    .AddEFMigrations("projects-migrations", "CloudGround.App.Modules.Projects.Persistence.ProjectsDbContext")
    .WithMigrationsProject("../Modules/CloudGround.App.Modules.Projects/CloudGround.App.Modules.Projects.csproj")
    .WithReference(database)
    .WaitFor(database)
    .RunDatabaseUpdateOnStart();

var tasksMigrations = api
    .AddEFMigrations("tasks-migrations", "CloudGround.App.Modules.Tasks.Persistence.TasksDbContext")
    .WithMigrationsProject("../Modules/CloudGround.App.Modules.Tasks/CloudGround.App.Modules.Tasks.csproj")
    .WithReference(database)
    .WaitFor(database)
    .RunDatabaseUpdateOnStart();

api.WaitForCompletion(projectsMigrations)
    .WaitForCompletion(tasksMigrations);
#endif

builder.Build().Run();
