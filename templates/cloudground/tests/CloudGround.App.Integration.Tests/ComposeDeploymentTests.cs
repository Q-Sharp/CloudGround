namespace CloudGround.App.Integration.Tests;

public sealed class ComposeDeploymentTests
{
    private const string Started = "service_started";

    [Fact]
    public void Dependency_on_a_service_outside_the_file_keeps_its_condition()
    {
        var api = new Service { Name = "api" };
        api.DependsOn["identity"] = new ServiceDependency { Condition = Started };
        var composeFile = new ComposeFile();
        composeFile.Services["api"] = api;

        ComposeDeployment.Configure(composeFile);

        Assert.Equal(Started, api.DependsOn["identity"].Condition);
    }
}
