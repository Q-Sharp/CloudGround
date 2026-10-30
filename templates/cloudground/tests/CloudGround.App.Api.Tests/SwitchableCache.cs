namespace CloudGround.App.Api.Tests;

public enum CacheOutage
{
    None,
    Unreachable,
    TimedOut,
}

internal sealed class SwitchableCache : IDistributedCache
{
    private readonly MemoryDistributedCache cache = new(Microsoft.Extensions.Options.Options.Create(new MemoryDistributedCacheOptions()));

    public CacheOutage Outage { get; set; }

    public byte[]? Get(string key) => Reachable().Get(key);

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Reachable().GetAsync(key, token);

    public void Refresh(string key) => Reachable().Refresh(key);

    public Task RefreshAsync(string key, CancellationToken token = default) => Reachable().RefreshAsync(key, token);

    public void Remove(string key) => Reachable().Remove(key);

    public Task RemoveAsync(string key, CancellationToken token = default) => Reachable().RemoveAsync(key, token);

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => Reachable().Set(key, value, options);

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) =>
        Reachable().SetAsync(key, value, options, token);

    private MemoryDistributedCache Reachable() => Outage switch
    {
        CacheOutage.Unreachable => throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, "The cache is unavailable."),
        CacheOutage.TimedOut => throw new TimeoutException("The cache timed out."),
        _ => cache,
    };
}
