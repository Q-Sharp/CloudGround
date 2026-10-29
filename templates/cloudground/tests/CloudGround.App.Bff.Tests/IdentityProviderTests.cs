using CloudGround.App.Bff.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CloudGround.App.Bff.Tests;

public sealed class IdentityProviderTests
{
    [Theory]
    [InlineData("https://id.example.com")]
    [InlineData("https://id.example.com/")]
    public void Sign_in_uses_the_configured_identity_provider_outside_development(string identityProviderUrl)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Production });
        builder.Configuration["Keycloak:Url"] = identityProviderUrl;
        builder.Configuration["ConnectionStrings:cache"] = "127.0.0.1:1,connectTimeout=100";
        builder.AddRedisDistributedCache("cache");
        builder.AddBffAuthentication();
        using var services = builder.Services.BuildServiceProvider();

        var options = services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get(OpenIdConnectDefaults.AuthenticationScheme);

        Assert.Equal("https://id.example.com/realms/cloudground-app", options.Authority);
        Assert.True(options.RequireHttpsMetadata);
    }
}
