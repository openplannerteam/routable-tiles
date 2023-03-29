using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using OsmSharp;
using OsmSharp.Streams;

namespace RoutableTiles.API.Db.Caches.Disk;

public class SnapshotCommitTilesDiskCache
{
    private readonly SnapshotCommitTilesDiskCacheSettings _settings;
    private readonly ConcurrentDictionary<long, string> _queued = new();
    private readonly ILogger<SnapshotCommitTilesDiskCache> _logger;

    private const string Extension = "osm.bin";

    public SnapshotCommitTilesDiskCache(SnapshotCommitTilesDiskCacheSettings settings, ILogger<SnapshotCommitTilesDiskCache> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<(bool success, IReadOnlyList<OsmGeo> data)> TryFetchAsync(Guid snapshotCommitId,
        long tile)
    {
        if (string.IsNullOrEmpty(_settings.CachePath)) return (false, ArraySegment<OsmGeo>.Empty);

        // check the on-disk cache.
        var snapshotCommitCache = Path.Combine(_settings.CachePath, $"snapshot-commit-{snapshotCommitId}");
        var cachedTile = Path.Combine(snapshotCommitCache, $"{tile}.{Extension}");
        if (!File.Exists(cachedTile)) return (false, ArraySegment<OsmGeo>.Empty);

        while (true)
        {
            // if no hit, see if tile is already being fetched, if so wait.
            // ReSharper disable once InconsistentlySynchronizedField
            while (_queued.ContainsKey(tile))
            {
                _logger.LogDebug("Waiting for tile {TileId} {SnapshotCommitId} to be dequeued for read",
                    tile, snapshotCommitId);
                await Task.Delay(25);
            }

            // queue the tile but synchronize this to prevent more than on thread
            // fetching the same tile.
            lock (_queued)
            {
                if (_queued.ContainsKey(tile))
                {
                    // there are two threads at the same time trying to fetch this tile.
                    // wait for the other thread to finish.
                    _logger.LogDebug("Tile {TileId} {SnapshotCommitId} queued withing lock in the meantime for read", tile, snapshotCommitId);
                    continue;
                }
            }

            // fetch the tile.
            try
            {
                // check the on-disk cache.
                if (!File.Exists(cachedTile)) return (false, ArraySegment<OsmGeo>.Empty);

                // read data from disk.
                await using var cacheTileStream = File.OpenRead(cachedTile);
                using var osmBinarySource = new BinaryOsmStreamSource(cacheTileStream);
                var data = osmBinarySource.ToList();

                _logger.LogDebug("Read cached tile {TileId} {SnapshotCommitId} from disk", tile, snapshotCommitId);
                return (true, data);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to read tile from disk: {Tile} {SnapshotCommitId}",
                    tile, snapshotCommitId);

                // delete the file, it could be corrupt.
                File.Delete(cachedTile);
                return (false, ArraySegment<OsmGeo>.Empty);
            }
        }
    }

    public async Task SetAsync(Guid snapshotCommitId, long tile, IReadOnlyList<OsmGeo> data)
    {
        if (string.IsNullOrEmpty(_settings.CachePath)) return;

        while (true)
        {
            // if no hit, see if tile is already being fetched, if so wait.
            // ReSharper disable once InconsistentlySynchronizedField.
            while (_queued.ContainsKey(tile))
            {
                _logger.LogDebug("Waiting for tile {TileId} {SnapshotCommitId} to be dequeued for write",
                    tile, snapshotCommitId);
                await Task.Delay(25);
            }

            // queue the tile but synchronize this to prevent more than on thread
            // fetching the same tile.
            lock (_queued)
            {
                if (_queued.ContainsKey(tile))
                {
                    // there are two threads at the same time trying to fetch this tile.
                    // wait for the other thread to finish.
                    _logger.LogDebug("Tile {TileId} {SnapshotCommitId} queued withing lock in the meantime for write", tile, snapshotCommitId);
                    continue;
                }

                // queue the tile.
                _logger.LogDebug("Queued {TileId} {SnapshotCommitId} for write", tile, snapshotCommitId);
                _queued[tile] = string.Empty;
            }

            try
            {
                // create cache folder if needed.
                var snapshotCommitCache = Path.Combine(_settings.CachePath, $"snapshot-commit-{snapshotCommitId}");
                if (!Directory.Exists(snapshotCommitCache)) Directory.CreateDirectory(snapshotCommitCache);

                // write tile.
                var cachedTile = Path.Combine(snapshotCommitCache, $"{tile}.{Extension}");
                await using var cacheTileStream = File.Create(cachedTile);
                var osmBinaryTarget = new BinaryOsmStreamTarget(cacheTileStream);
                osmBinaryTarget.Initialize();
                osmBinaryTarget.RegisterSource(data);
                osmBinaryTarget.Pull();

                _logger.LogDebug("Written {TileId} {SnapshotCommitId} to disk", tile, snapshotCommitId);
                break;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to write tile to disk: {Tile} {SnapshotCommitId}",
                    tile, snapshotCommitId);
            }
            finally
            {
                // ReSharper disable once InconsistentlySynchronizedField
                _logger.LogDebug("Removed {TileId} {SnapshotCommitId} from queue", tile, snapshotCommitId);
                _queued.Remove(tile, out _);
            }
        }
    }

    /// <summary>
    /// Returns true if the tile exists.
    /// </summary>
    /// <param name="snapshotCommitId">The commit global id.</param>
    /// <param name="tile">The tile.</param>
    /// <returns>True if the tile exists.</returns>
    public async Task<bool> ExistsAsync(Guid snapshotCommitId, long tile)
    {
        // check the on-disk cache.
        var snapshotCommitCache = Path.Combine(_settings.CachePath, $"snapshot-commit-{snapshotCommitId}");
        var cachedTile = Path.Combine(snapshotCommitCache, $"{tile}.{Extension}");

        return File.Exists(cachedTile);
    }

    /// <summary>
    /// Enumerates all files that represent a cached tile.
    /// </summary>
    /// <returns>All tiles in the cache,</returns>
    public IEnumerable<(Guid snapshotCommitId, long tile)> EnumerateCacheEntries()
    {
        foreach (var snapshotPath in Directory.EnumerateDirectories(_settings.CachePath, "snapshot-commit-*").ToList())
        {
            // parse the snapshot id.
            var snapshotPathInfo = new DirectoryInfo(snapshotPath);
            var snapshotCommitId = Guid.Parse(snapshotPathInfo.Name["snapshot-commit-".Length..]);

            // enumerate all the tiles.
            foreach (var tileFile in Directory.EnumerateFiles(snapshotPath, $"*.{Extension}").ToList())
            {
                var tileFileName = Path.GetFileName(tileFile);
                var tileFileNameWithoutExtension = tileFileName[..tileFileName.IndexOf(".", StringComparison.Ordinal)];
                var tile = long.Parse(tileFileNameWithoutExtension);

                yield return (snapshotCommitId, tile);
            }
        }
    }
}
