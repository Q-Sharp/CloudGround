namespace CloudGround.App.Api.Tests;

internal sealed class UnreachableTokenEndpoint : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        throw new HttpRequestException("Connection refused");
}
