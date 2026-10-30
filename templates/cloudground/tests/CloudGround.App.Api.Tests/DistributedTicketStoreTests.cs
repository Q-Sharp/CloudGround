namespace CloudGround.App.Api.Tests;

public sealed class DistributedTicketStoreTests
{
    [Fact]
    public async Task Stored_ticket_can_be_retrieved_and_removed()
    {
        var store = new DistributedTicketStore(new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())), NullLogger<DistributedTicketStore>.Instance);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("preferred_username", "alice")], "test"));
        var ticket = new AuthenticationTicket(principal, new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1) }, "Cookies");

        var key = await store.StoreAsync(ticket);
        var retrieved = await store.RetrieveAsync(key);
        await store.RemoveAsync(key);

        Assert.StartsWith("sessions/", key, StringComparison.Ordinal);
        Assert.Equal("alice", retrieved?.Principal.FindFirst("preferred_username")?.Value);
        Assert.Null(await store.RetrieveAsync(key));
    }
}
