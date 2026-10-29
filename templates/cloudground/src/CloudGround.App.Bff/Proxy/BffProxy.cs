using Microsoft.AspNetCore.Authentication;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

namespace CloudGround.App.Bff.Proxy;

internal static class BffProxy
{
    public static RouteConfig ApiRoute { get; } = new()
    {
        RouteId = "api",
        ClusterId = "api",
        Order = 0,
        Match = new RouteMatch { Path = "/api/{**rest}" },
    };

    public static ClusterConfig ApiCluster { get; } = Cluster("api", "http://api");

    public static RouteConfig WebRoute { get; } = new()
    {
        RouteId = "web",
        ClusterId = "web",
        Order = int.MaxValue,
        Match = new RouteMatch { Path = "/{**path}" },
    };

    public static ClusterConfig WebCluster { get; } = Cluster("web", "http://web");

    public static WebApplicationBuilder AddBffProxy(this WebApplicationBuilder builder)
    {
        builder.Services.AddReverseProxy()
            .LoadFromMemory([ApiRoute, WebRoute], [ApiCluster, WebCluster])
            .AddBffProxyTransforms()
            .AddServiceDiscoveryDestinationResolver();

        return builder;
    }

    public static IReverseProxyBuilder AddBffProxyTransforms(this IReverseProxyBuilder proxy) =>
        proxy.AddTransforms(context =>
        {
            context.AddRequestHeaderRemove("Cookie");
            context.AddRequestHeaderRemove("Authorization");
            if (context.Route.RouteId != ApiRoute.RouteId)
            {
                return;
            }

            context.AddRequestTransform(async transform =>
            {
                if (await transform.HttpContext.GetTokenAsync("access_token") is { } accessToken)
                {
                    transform.ProxyRequest.Headers.Authorization = new("Bearer", accessToken);
                }
            });
        });

    private static ClusterConfig Cluster(string id, string address) => new()
    {
        ClusterId = id,
        Destinations = new Dictionary<string, DestinationConfig> { [id] = new() { Address = address } },
    };
}
