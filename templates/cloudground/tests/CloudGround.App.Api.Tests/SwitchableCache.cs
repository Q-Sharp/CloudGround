using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CloudGround.App.Api.Tests;

internal sealed class SwitchableCache : IDistributedCache
{
    private readonly MemoryDistributedCache cache = new(Options.Create(new MemoryDistributedCacheOptions()));

    public bool Available { get; set; } = true;

    public byte[]? Get(string key) => Reachable().Get(key);

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Reachable().GetAsync(key, token);

    public void Refresh(string key) => Reachable().Refresh(key);

    public Task RefreshAsync(string key, CancellationToken token = default) => Reachable().RefreshAsync(key, token);

    public void Remove(string key) => Reachable().Remove(key);

    public Task RemoveAsync(string key, CancellationToken token = default) => Reachable().RemoveAsync(key, token);

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => Reachable().Set(key, value, options);

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) =>
        Reachable().SetAsync(key, value, options, token);

    private MemoryDistributedCache Reachable() =>
        Available ? cache : throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, "The cache is unavailable.");
}
