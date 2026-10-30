using CloudGround.App.Api;
#if (sample)
using CloudGround.App.Modules.Projects;
using CloudGround.App.Modules.Tasks;
#endif
using JasperFx;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("cache");
builder.Services.AddHybridCache();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.AddMessaging();
builder.AddApiAuthentication();
builder.AddXsrfProtection();
#if (sample)
builder.AddProjectsModule();
builder.AddTasksModule();
#endif

var app = builder.Build();

app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = exception => exception is BadHttpRequestException badRequest
        ? badRequest.StatusCode
        : StatusCodes.Status500InternalServerError,
});
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.UseXsrfProtection();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapDefaultEndpoints();
app.MapAccountEndpoints();
#if (sample)
app.MapProjectsModule();
app.MapTasksModule();
#endif

return await app.RunJasperFxCommands(args);
