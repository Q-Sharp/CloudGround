var builder = DistributedApplication.CreateBuilder(args);

var database = builder.AddPostgres("postgres").AddDatabase("database");
var cache = builder.AddGarnet("cache");

builder.AddProject<Projects.CloudGround_App_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(cache)
    .WaitFor(cache);

builder.Build().Run();
