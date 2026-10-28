using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CloudGround.App.Integration.Tests;

public sealed class BffSessionTests(AppHostFixture fixture)
{
    private const int Requests = 10;

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Session_works_on_every_bff_instance()
    {
        using var session = await BffLogin.LogInAsync(fixture, "alice");

        for (var request = 0; request < Requests; request++)
        {
            using var onNewConnection = new HttpRequestMessage(HttpMethod.Get, new Uri("/bff/user", UriKind.Relative));
            onNewConnection.Headers.ConnectionClose = true;

            using var response = await session.Client.SendAsync(onNewConnection, Cancellation);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("alice", (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("name").GetString());
        }
    }
}
