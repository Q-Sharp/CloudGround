namespace CloudGround.App.Bff.Tests;

internal sealed class UnreachableTokenEndpoint : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        throw new HttpRequestException("Connection refused");
}
