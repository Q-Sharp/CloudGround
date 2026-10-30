using CloudGround.App.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("compose")
    .WithDashboard(ComposeDeployment.UseReleasedDashboard)
    .ConfigureComposeFile(ComposeDeployment.Configure);

var database = builder.AddPostgres("postgres")
    .WithDataVolume(DeploymentVolume("postgres-data"))
    .PublishAsDockerComposeService((_, service) => service.Healthcheck = ComposeDeployment.Probe("pg_isready", "--host", "127.0.0.1", "--username", "postgres"))
    .AddDatabase("database");
var cache = builder.AddGarnet("cache").WithDataVolume(DeploymentVolume("cache-data"));
var messaging = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin()
    .PublishAsDockerComposeService((_, service) => service.Healthcheck = ComposeDeployment.Probe("rabbitmq-diagnostics", "--quiet", "ping"));
var bffClientSecret = builder.AddParameter("bff-client-secret", new GenerateParameterDefault { MinLength = 32, Special = false }, secret: true, persist: true);

var api = builder.AddProject<Projects.CloudGround_App_Api>("api")
    .BuildImageAfterMigrationBundles()
    .WithHttpHealthCheck("/health", endpointName: "http")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(messaging)
    .WaitFor(messaging);

var web = builder.AddJavaScriptApp("web", "../CloudGround.App.Web")
    .WithNpm()
    .WithHttpEndpoint(env: "PORT")
    .WithHttpHealthCheck("/health")
    .WithReference(api)
    .WaitFor(api);

var bff = builder.AddProject<Projects.CloudGround_App_Bff>("bff")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health", endpointName: "http")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(api)
    .WaitFor(api)
    .WithReference(web)
    .WithEnvironment("Bff__ClientSecret", bffClientSecret);

if (builder.ExecutionContext.IsRunMode)
{
#pragma warning disable ASPIRECERTIFICATES001
    var keycloak = builder.AddKeycloak("keycloak", port: 8380)
        .WithDataVolume()
        .WithoutHttpsCertificate()
        .WithRealmImport("./Realms")
        .WithEnvironment("BFF_CLIENT_SECRET", bffClientSecret);
#pragma warning restore ASPIRECERTIFICATES001
    api.WithReference(keycloak).WaitFor(keycloak);
    bff.WithReference(keycloak).WaitFor(keycloak);
    web.WithEnvironment("NODE_OPTIONS", "--import ./src/instrumentation.mjs")
        .WithArgs("--", "--port", web.GetEndpoint("http").Property(EndpointProperty.TargetPort));
}
else
{
    var keycloakUrl = builder.AddParameter("keycloak-url");
    api.WithEnvironment("Keycloak__Url", keycloakUrl);
    bff.WithEnvironment("Keycloak__Url", keycloakUrl);
    web.WithEnvironment("NG_ALLOWED_HOSTS", web.GetEndpoint("http").Property(EndpointProperty.Host));
}

#if (sample)
api.WithModuleMigrations("Projects", database)
    .WithModuleMigrations("Tasks", database);
#endif

builder.TagImagesWithVersion();

builder.Build().Run();

string? DeploymentVolume(string name) => builder.ExecutionContext.IsPublishMode ? name : null;
