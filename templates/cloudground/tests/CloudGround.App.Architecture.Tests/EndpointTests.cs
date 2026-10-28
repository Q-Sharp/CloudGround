using Microsoft.AspNetCore.Mvc;

namespace CloudGround.App.Architecture.Tests;

public sealed class EndpointTests
{
    [Fact]
    public void No_endpoint_is_a_controller()
    {
        var controllers = ModuleCatalog.HostAndModules
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type) || type.IsDefined(typeof(ApiControllerAttribute), inherit: true));

        Assert.Empty(controllers);
    }
}
