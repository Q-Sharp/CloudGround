namespace CloudGround.App.Integration.Tests;

public sealed class SessionTests(AppHostFixture fixture)
{
    private const int Requests = 10;

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Session_works_on_every_api_instance()
    {
        using var session = await BrowserLogin.LogInAsync(fixture, "alice");

        for (var request = 0; request < Requests; request++)
        {
            using var onNewConnection = new HttpRequestMessage(HttpMethod.Get, new Uri("/account/user", UriKind.Relative));
            onNewConnection.Headers.ConnectionClose = true;

            using var response = await session.Client.SendAsync(onNewConnection, Cancellation);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("alice", (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("name").GetString());
        }
    }

    [Fact]
    public async Task Sessions_and_data_protection_keys_live_in_garnet()
    {
        using var session = await BrowserLogin.LogInAsync(fixture, "alice");

        await using var garnet = await ConnectionMultiplexer.ConnectAsync((await fixture.App.GetConnectionStringAsync("cache", Cancellation))!);
        var sessions = await garnet.GetServers().Single().KeysAsync(pattern: "sessions/*").ToListAsync(Cancellation);

        Assert.True(await garnet.GetDatabase().KeyExistsAsync("data-protection-keys"));
        Assert.NotEmpty(sessions);
    }
}
