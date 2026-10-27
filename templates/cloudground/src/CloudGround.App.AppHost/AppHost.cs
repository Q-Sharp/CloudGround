var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.CloudGround_App_Api>("api");

builder.Build().Run();
