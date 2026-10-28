using CloudGround.App.Bff.Antiforgery;
using CloudGround.App.Bff.Authentication;
using CloudGround.App.Bff.Endpoints;
using CloudGround.App.Bff.Proxy;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("cache");
builder.Services.AddProblemDetails();
builder.AddBffAuthentication();
builder.AddXsrfProtection();
builder.AddApiProxy();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.UseXsrfProtection();

app.MapDefaultEndpoints();
app.MapBffEndpoints();
app.MapReverseProxy();

app.Run();
