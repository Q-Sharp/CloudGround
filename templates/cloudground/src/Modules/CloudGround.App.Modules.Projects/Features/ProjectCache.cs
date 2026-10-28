using Microsoft.Extensions.Caching.Hybrid;

namespace CloudGround.App.Modules.Projects.Features;

internal static class ProjectCache
{
    public static HybridCacheEntryOptions SharedAcrossInstances { get; } = new()
    {
        Flags = HybridCacheEntryFlags.DisableLocalCache,
    };

    public static string Key(Guid projectId) => $"projects/{projectId}";
}
