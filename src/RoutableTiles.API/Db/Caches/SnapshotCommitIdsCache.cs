using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace RoutableTiles.API.Db.Caches;

public class SnapshotCommitIdsCache
{
    private readonly IMemoryCache _tileDataCache;
    private readonly ILogger<SnapshotCommitIdsCache> _logger;
    private readonly List<long> _empty = new();

    public SnapshotCommitIdsCache(SnapshotCommitIdsCacheSettings settings,
        ILogger<SnapshotCommitIdsCache> logger)
    {
        _logger = logger;

        _tileDataCache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = settings.CacheSize
        });
    }

    public (bool success, List<long> commitIdsBefore) TryFetchFromCache(long snapshotCommitId)
    {
        return _tileDataCache.TryGetValue(snapshotCommitId, out List<long> commitIdsBefore) ? (true, commitIdsBefore) : (false, _empty);
    }

    public void Set(long snapshotCommitId, List<long> commitIdsBefore)
    {
        // add to cache.
        _tileDataCache.Set(snapshotCommitId, commitIdsBefore, new MemoryCacheEntryOptions()
        {
            Size = 1
        });
    }
}
