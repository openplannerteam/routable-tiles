using Microsoft.Extensions.Caching.Memory;
using OsmSharp;
using RoutableTiles.API.Db.Caches.Disk;

namespace RoutableTiles.API.Db.Caches;

public class SnapshotCommitTilesCache
{
    private readonly SnapshotCommitTilesDiskCache _diskCache;
    private readonly IMemoryCache _tileDataCache;
    private readonly ILogger<SnapshotCommitTilesCache> _logger;

    public SnapshotCommitTilesCache(SnapshotCommitTilesCacheSettings settings,
        SnapshotCommitTilesDiskCache diskCache, ILogger<SnapshotCommitTilesCache> logger)
    {
        _logger = logger;
        _diskCache = diskCache;

        _tileDataCache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = settings.CacheSize
        });
    }

    public async Task<(bool success, IReadOnlyList<OsmGeo> data)> TryFetchFromCache(Guid snapshotCommitId, long tile)
    {
        // first try cache.
        var cacheKey = (snapshotCommitId, tile);
        if (_tileDataCache.TryGetValue(cacheKey, out IReadOnlyList<OsmGeo> cached))
        {
            return (true, cached);
        }

        // try to fetch from disk.
        _logger.LogDebug("Cache miss for tile {TileId}, trying disk", tile);
        var (success, data) = await _diskCache.TryFetchAsync(snapshotCommitId, tile);
        if (success)
        {
            // fetch from disks succeeded, add to memory cache.
            _logger.LogDebug("Got tile {TileId}, adding to memory cache", tile);
            _tileDataCache.Set(cacheKey, data, new MemoryCacheEntryOptions()
            {
                Size = 1
            });
        }

        return (success, data);
    }

    public async Task Set(Guid snapshotCommitId, long tile, IReadOnlyList<OsmGeo> data)
    {
        var cacheKey = (snapshotCommitId, tile);

        // the tile has been fetched, try the cache first.
        if (_tileDataCache.TryGetValue(cacheKey, out _))
        {
            return;
        }

        // add to cache.
        _tileDataCache.Set(cacheKey, data, new MemoryCacheEntryOptions()
        {
            Size = 1
        });

        // write to disk.
        await _diskCache.SetAsync(snapshotCommitId, tile, data);
    }
}
