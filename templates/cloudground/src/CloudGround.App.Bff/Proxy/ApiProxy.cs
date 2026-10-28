using Microsoft.AspNetCore.Authentication;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

namespace CloudGround.App.Bff.Proxy;

internal static class ApiProxy
{
    private const string ApiServiceAddress = "http://api";

    public static RouteConfig Route { get; } = new()
    {
        RouteId = "api",
        ClusterId = "api",
        Match = new RouteMatch { Path = "/api/{**rest}" },
    };

    public static ClusterConfig Cluster { get; } = new()
    {
        ClusterId = "api",
        Destinations = new Dictionary<string, DestinationConfig> { ["api"] = new() { Address = ApiServiceAddress } },
    };

    public static WebApplicationBuilder AddApiProxy(this WebApplicationBuilder builder)
    {
        builder.Services.AddReverseProxy()
            .LoadFromMemory([Route], [Cluster])
            .AddApiProxyTransforms()
            .AddServiceDiscoveryDestinationResolver();

        return builder;
    }

    public static IReverseProxyBuilder AddApiProxyTransforms(this IReverseProxyBuilder proxy) =>
        proxy.AddTransforms(context =>
        {
            if (context.Route.RouteId != Route.RouteId)
            {
                return;
            }

            context.AddRequestHeaderRemove("Cookie");
            context.AddRequestHeaderRemove("Authorization");
            context.AddRequestTransform(async transform =>
            {
                if (await transform.HttpContext.GetTokenAsync("access_token") is { } accessToken)
                {
                    transform.ProxyRequest.Headers.Authorization = new("Bearer", accessToken);
                }
            });
        });
}
