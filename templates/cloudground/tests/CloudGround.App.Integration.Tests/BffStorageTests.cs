using StackExchange.Redis;

namespace CloudGround.App.Integration.Tests;

public sealed class BffStorageTests(AppHostFixture fixture)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Sessions_and_data_protection_keys_live_in_garnet()
    {
        using var session = await BffLogin.LogInAsync(fixture, "alice");

        await using var garnet = await ConnectionMultiplexer.ConnectAsync((await fixture.App.GetConnectionStringAsync("cache", Cancellation))!);
        var tickets = await garnet.GetServers().Single().KeysAsync(pattern: "bff/tickets/*").ToListAsync(Cancellation);

        Assert.True(await garnet.GetDatabase().KeyExistsAsync("bff/data-protection-keys"));
        Assert.NotEmpty(tickets);
    }
}
