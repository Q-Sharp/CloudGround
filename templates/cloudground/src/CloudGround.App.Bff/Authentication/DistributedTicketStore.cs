using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Distributed;

namespace CloudGround.App.Bff.Authentication;

internal sealed class DistributedTicketStore(IDistributedCache cache) : ITicketStore
{
    private const string KeyPrefix = "bff/tickets/";

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = $"{KeyPrefix}{Guid.NewGuid()}";
        await RenewAsync(key, ticket);
        return key;
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket) =>
        cache.SetAsync(key, TicketSerializer.Default.Serialize(ticket), new DistributedCacheEntryOptions { AbsoluteExpiration = ticket.Properties.ExpiresUtc });

    public async Task<AuthenticationTicket?> RetrieveAsync(string key) =>
        await cache.GetAsync(key) is { } bytes ? TicketSerializer.Default.Deserialize(bytes) : null;

    public Task RemoveAsync(string key) => cache.RemoveAsync(key);
}
