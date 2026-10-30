using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Yarp.ReverseProxy.Transforms.Builder;

namespace CloudGround.App.Api.Tests;

public sealed class WebProxyTests
{
    private const string Destination = "http://web.example";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Forwarded_requests_carry_no_cookie_or_authorization()
    {
        var services = new ServiceCollection().AddLogging();
        services.AddReverseProxy().AddWebProxyTransforms();
        await using var provider = services.BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/explore";
        context.Request.Headers.Cookie = "cloudground-app-session=key; cloudground-app-xsrf=token";
        context.Request.Headers.Authorization = "Bearer token";

        var transformer = provider.GetRequiredService<ITransformBuilder>().Build(WebProxy.WebRoute, WebProxy.WebCluster);
        using var proxyRequest = new HttpRequestMessage(HttpMethod.Get, Destination);
        await transformer.TransformRequestAsync(context, proxyRequest, Destination, Cancellation);

        Assert.False(proxyRequest.Headers.Contains("Cookie"));
        Assert.Null(proxyRequest.Headers.Authorization);
    }

    [Fact]
    public async Task Web_route_maps_with_the_unresolved_configuration_of_code_generation()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Production });
        builder.Configuration["services:web:http:0"] = "{web.bindings.http.url}";
        builder.AddServiceDefaults();
        builder.AddRedisDistributedCache("cache");
        builder.AddApiAuthentication();
        builder.AddWebProxy();
        await using var app = builder.Build();

        var exception = Record.Exception(() => app.MapWebProxy());

        Assert.Null(exception);
    }

    [Fact]
    public void Web_route_comes_after_every_endpoint_of_the_api()
    {
        Assert.Equal(int.MaxValue, WebProxy.WebRoute.Order);
        Assert.Equal("/{**path}", WebProxy.WebRoute.Match.Path);
    }
}
