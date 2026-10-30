namespace CloudGround.App.Api;

internal static class WebProxy
{
    private static readonly string[] PathsOfTheApi = ["/api", "/account"];

    public static RouteConfig WebRoute { get; } = new()
    {
        RouteId = "web",
        ClusterId = "web",
        Order = int.MaxValue,
        Match = new RouteMatch { Path = "/{**path}" },
    };

    public static ClusterConfig WebCluster { get; } = new()
    {
        ClusterId = "web",
        Destinations = new Dictionary<string, DestinationConfig> { ["web"] = new() { Address = "http://web" } },
    };

    public static WebApplicationBuilder AddWebProxy(this WebApplicationBuilder builder)
    {
        builder.Services.AddReverseProxy()
            .LoadFromMemory([WebRoute], [WebCluster])
            .AddWebProxyTransforms();
        builder.Services.AddServiceDiscoveryForwarderFactory();

        return builder;
    }

    public static IReverseProxyBuilder AddWebProxyTransforms(this IReverseProxyBuilder proxy) =>
        proxy.AddTransforms(context =>
        {
            context.AddRequestHeaderRemove("Cookie");
            context.AddRequestHeaderRemove("Authorization");
        });

    public static IEndpointRouteBuilder MapWebProxy(this IEndpointRouteBuilder endpoints)
    {
        foreach (var path in PathsOfTheApi)
        {
            endpoints.Map($"{path}/{{**rest}}", () => TypedResults.NotFound()).ExcludeFromDescription();
        }

        endpoints.MapReverseProxy();
        return endpoints;
    }
}
