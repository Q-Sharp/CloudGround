using CloudGround.App.Bff.Proxy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms.Builder;

namespace CloudGround.App.Bff.Tests;

public sealed class BffProxyTests
{
    private const string Destination = "http://api.example";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Forwarded_requests_carry_no_bff_cookies()
    {
        using var proxyRequest = await ForwardAsync(accessToken: "access-token");

        Assert.False(proxyRequest.Headers.Contains("Cookie"));
    }

    [Fact]
    public async Task Forwarded_requests_carry_the_access_token()
    {
        using var proxyRequest = await ForwardAsync(accessToken: "access-token");

        Assert.Equal("Bearer", proxyRequest.Headers.Authorization?.Scheme);
        Assert.Equal("access-token", proxyRequest.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task Pages_are_forwarded_without_tokens()
    {
        using var proxyRequest = await ForwardAsync(accessToken: "access-token", route: BffProxy.WebRoute, cluster: BffProxy.WebCluster);

        Assert.False(proxyRequest.Headers.Contains("Cookie"));
        Assert.Null(proxyRequest.Headers.Authorization);
    }

    [Fact]
    public async Task Anonymous_requests_carry_no_authorization_header()
    {
        using var proxyRequest = await ForwardAsync(accessToken: null);

        Assert.Null(proxyRequest.Headers.Authorization);
    }

    private static async Task<HttpRequestMessage> ForwardAsync(string? accessToken, RouteConfig? route = null, ClusterConfig? cluster = null)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IAuthenticationService>(new SignedInUser(accessToken));
        services.AddReverseProxy().AddBffProxyTransforms();
        await using var provider = services.BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/projects";
        context.Request.Headers.Cookie = "bff=session; XSRF-TOKEN=token";
        context.Request.Headers.Authorization = "Bearer forged-by-the-browser";

        var transformer = provider.GetRequiredService<ITransformBuilder>().Build(route ?? BffProxy.ApiRoute, cluster ?? BffProxy.ApiCluster);
        var proxyRequest = new HttpRequestMessage(HttpMethod.Get, Destination);
        await transformer.TransformRequestAsync(context, proxyRequest, Destination, Cancellation);
        return proxyRequest;
    }
}
