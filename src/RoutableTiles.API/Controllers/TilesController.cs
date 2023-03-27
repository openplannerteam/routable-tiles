using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using OsmSharp;
using RoutableTiles.API.Db;
using RoutableTiles.API.Db.Caches;
using RoutableTiles.API.Db.Tiles;
using RoutableTiles.API.Services.LatestCommit;

namespace RoutableTiles.API.Controllers;

/// <summary>
/// Controller to return routable tiles.
/// </summary>
/// <remarks>
/// This implements the memento: http://www.mementoweb.org/guide/howto/
///
/// The generic URI: {z}/{x}/{y} will redirect to the latest version URI.
/// The version URI: {timestamp}/{z}/{x}/{y}
/// 
/// </remarks>
[Route("/")]
[ApiController]
public class TilesController : ControllerBase
{
    private readonly OsmDbContext _db;
    private readonly LatestCommitStore _latestCommitStore;
    private readonly SnapshotCommitTilesCache _tilesCache;

    public TilesController(OsmDbContext db, LatestCommitStore latestCommitStore, SnapshotCommitTilesCache tilesCache)
    {
        _db = db;
        _latestCommitStore = latestCommitStore;
        _tilesCache = tilesCache;
    }

    /// <summary>
    /// Returns a redirect to the latest version of the request a tile.
    /// </summary>
    /// <param name="z"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    [HttpGet("{z:int}/{x:int}/{y:int}/")]
    public async Task<ActionResult> GetJsonLdRedirect(int z, int x, int y)
    {
        // read-only, don't track changes.
        _db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

        // get the global last commit.
        var commit = await _latestCommitStore.GetLatest(_db);
        if (commit == null) return this.NotFound();

        // get the latest commit the tile has changed in.
        var tile = new Tile(x, y, z);
        var latestCommit = await _db.GetLatestSnapshotCommitForTile(commit, (long)tile.Id);

        // this data is immutable, even the redirects and can be cached forever.
        this.Response.Headers[HeaderNames.CacheControl] = "public,max-age=" + (int)TimeSpan.FromDays(356).TotalSeconds;
        this.Response.Headers[HeaderNames.LastModified] = commit.Timestamp.ToUniversalTime().ToString("R");

        // return a redirect to the previous commit.
        var routeValues = new RouteValueDictionary
        {
            ["timestamp"] = latestCommit.Timestamp.FloorMinute().ToUniversalTime().ToString("yyyyMMdd-HHmmss"),
            ["x"] = x,
            ["y"] = y,
            ["z"] = z
        };
        return this.RedirectToAction(nameof(GetJsonLdAt), routeValues);
    }

    [HttpGet("{timestamp}/{z:int}/{x:int}/{y:int}/")]
    public async Task<ActionResult<IReadOnlyList<OsmGeo>>> GetJsonLdAt(string timestamp, int z, int x, int y)
    {
        // read-only, don't track changes.
        _db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        
        // parse the timestamp and make sure it's UTC.
        if (!DateTime.TryParseExact(timestamp, "yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out var utcDate))
        {
            return this.NotFound();
        }

        utcDate = utcDate.ToUniversalTime();
        
        // try to get commit from cache.
        var commit = await _db.SnapshotCommitsByTimestampCache.GetFor(utcDate);
        if (commit == null)
        {
            // get the closest commit.
            commit = await _db.SnapshotCommits
                .Where(c => c.Timestamp < utcDate.FloorMinute().AddMinutes(1))
                .FirstOrDefaultAsync();
            if (commit == null) return this.NotFound("No data before the given timestamp");
            
            _db.SnapshotCommitsByTimestampCache.Set(commit);
        }

        // get the latest commit the tile has changed in.
        var tile = new Tile(x, y, z);
        var latestCommit = await _db.GetLatestSnapshotCommitForTile(commit, (long)tile.Id);
        
        // if latest is not this there is an
        if (latestCommit.Id != commit.Id)
        {
            var routeValues = new RouteValueDictionary
            {
                ["timestamp"] = latestCommit.Timestamp.ToUniversalTime().ToString("yyyyMMdd-HHmmss"),
                ["x"] = x,
                ["y"] = y,
                ["z"] = z
            };
            return this.RedirectToAction(nameof(GetJsonLdAt), routeValues);
        }

        // get the tile using the cache if possible.
        return new ActionResult<IReadOnlyList<OsmGeo>>(await _db.GetTileCached(commit, _tilesCache, (long)tile.Id));
    }
}