namespace CloudGround.App.Modules.ModuleName.Tests;

public sealed class ModuleNameModuleTests
{
    [Fact]
    public async Task Editing_requires_the_editor_role()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["ConnectionStrings:database"] = "Host=localhost";
        builder.AddModuleNameModule();
        using var host = builder.Build();

        var policy = await host.Services.GetRequiredService<IAuthorizationPolicyProvider>().GetPolicyAsync(ModuleNamePolicies.Edit);

        Assert.NotNull(policy);
        Assert.Equal("editor", Assert.Single(Assert.Single(policy.Requirements.OfType<RolesAuthorizationRequirement>()).AllowedRoles));
    }
}
