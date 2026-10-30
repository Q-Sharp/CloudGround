namespace CloudGround.App.Api;

internal sealed partial class DistributedTicketStore(IDistributedCache cache, ILogger<DistributedTicketStore> logger) : ITicketStore
{
    private const string KeyPrefix = "sessions/";

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = $"{KeyPrefix}{Guid.NewGuid()}";
        await RenewAsync(key, ticket);
        return key;
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket) =>
        cache.SetAsync(key, TicketSerializer.Default.Serialize(ticket), new DistributedCacheEntryOptions { AbsoluteExpiration = ticket.Properties.ExpiresUtc });

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        try
        {
            return await cache.GetAsync(key) is { } bytes ? TicketSerializer.Default.Deserialize(bytes) : null;
        }
        catch (Exception exception) when (exception is RedisException or TimeoutException)
        {
            LogSessionStoreUnavailable(logger, exception);
            return null;
        }
    }

    public Task RemoveAsync(string key) => cache.RemoveAsync(key);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The session store is unavailable, so the request continues without a session.")]
    private static partial void LogSessionStoreUnavailable(ILogger logger, Exception exception);
}
