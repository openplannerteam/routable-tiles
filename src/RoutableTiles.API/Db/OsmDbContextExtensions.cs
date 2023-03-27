using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using OsmSharp;
using RoutableTiles.API.Db.Caches;
using RoutableTiles.API.Db.Tables;
using RoutableTiles.API.Db.Tiles;

namespace RoutableTiles.API.Db;

public static class OsmDbContextExtensions
{
    /// <summary>
    /// Gets the nodes, ways and relations in the given tile at the given snapshot commit using the cache if possible.
    /// </summary>
    /// <remarks>
    /// Makes sure the tile cached is from the latest commit containing the tile but returns the data as if it's from the current commit.
    /// </remarks>
    /// <param name="db">The db.</param>
    /// <param name="commit">The snapshot commit.</param>
    /// <param name="tile">The tile id.</param>
    /// <param name="tilesCache">The tiles cache.</param>
    /// <returns>The nodes, ways and relations in the tile.</returns>
    /// <exception cref="ArgumentException"></exception>
    public static async Task<IReadOnlyList<OsmGeo>> GetTileCached(this OsmDbContext db, SnapshotCommit commit,
        SnapshotCommitTilesCache tilesCache, long tile)
    {
        // check the tile for the data.
        var (success, data) = await tilesCache.TryFetchFromCache(commit.GlobalId, tile);
        if (success) return data;

        // get the latest commit containing this tile.
        var latestCommit = await db.GetLatestSnapshotCommitForTile(commit, tile);
        if (latestCommit.Id != commit.Id)
        {
            // retry cache with the proper commit.
            (success, data) = await tilesCache.TryFetchFromCache(latestCommit.GlobalId, tile);
            if (success) return data;
        }

        // get the tile data.
        data = (await db.GetTile(latestCommit, tile)).ToList();

        // add to cache.
        await tilesCache.Set(latestCommit.GlobalId, tile, data);

        return data;
    }

    /// <summary>
    /// Gets the nodes, ways and relations in the given tile at the given snapshot commit.
    /// </summary>
    /// <param name="db">The db.</param>
    /// <param name="commit">The snapshot commit.</param>
    /// <param name="tileId">The tile id.</param>
    /// <returns>The nodes, ways and relations in the tile.</returns>
    /// <exception cref="ArgumentException"></exception>
    public static async Task<IEnumerable<OsmGeo>> GetTile(this OsmDbContext db, SnapshotCommit commit, long tileId)
    {
        var tile = new Tile((ulong)tileId);

        var tileIds = new HashSet<long> { tileId };

        return await db.GetInBox(commit, tile.Left, tile.Bottom, tile.Right, tile.Top, tileIds);
    }
    /// <summary>
    /// Gets the nodes, ways and relations in the given bbox for the given commit.
    /// </summary>
    /// <param name="db">The db.</param>
    /// <param name="commit">The snapshot commit.</param>
    /// <param name="left">The left.</param>
    /// <param name="bottom">The bottom.</param>
    /// <param name="right">The right.</param>
    /// <param name="top">The top.</param>
    /// <returns>The nodes, ways and relations in the given bbox.</returns>
    /// <exception cref="ArgumentException"></exception>
    internal static async Task<IEnumerable<OsmGeo>> GetInBox(this OsmDbContext db, SnapshotCommit commit,
        double left, double bottom, double right,
        double top, HashSet<long> tileIds)
    {
        // getting all the data in the bbox works in the following way:

        // STEP1: get all nodes:
        // 1. get all nodes from snapshot.
        // 2. get all nodes from branch by getting:
        //   - all the nodes in the bbox.
        //   - all the nodes with an id that also occurs in the snapshot (they could have been moved out of the bbox)
        //     -> remove all the nodes in this set, they are not in the box anymore.
        // 3. merge by:
        //   - removing nodes deleted in branch.
        //   - replacing nodes that have a new version in branch.

        // STEP2: get all ways
        // 1. get all ways from snapshot with at least one node in box.
        // 2. get all ways from branch by getting:
        //   - all the ways with at least one node in box.
        //   - all the ways with an id also in the snapshot (they could have had some nodes deleted)
        //     -> remove all the ways in this set, they are not in the box anymore.
        // 3. merge by:
        //   - removing ways deleted in branch.
        //   - replacing ways that have a new version in branch.

        // STEP3: get all relations
        // 1. get all relations from snapshot with at least one member in box.
        // 2. get all relations from branch by getting:
        //   - all the relations with at least one member in box.
        //   - all the relations with an id also in the snapshot (they could have had some members deleted)
        //     -> remove all the relations in this set, they are not in the box anymore.
        // 3. merge by:
        //   - removing relations deleted in branch.
        //   - replacing relations that have a new version in branch.

        var data = new List<OsmGeo>();

        // do nodes.
        var nodesInSnapshot =
            await db.GetSnapshotNodesInBoxAsync(commit.Id, left, bottom, right, top, tileIds);
        bool IsInBox(Node node)
        {
            return node.Latitude >= bottom && node.Latitude < top && node.Longitude >= left &&
                   node.Longitude < right;
        }

        foreach (var node in nodesInSnapshot)
        {
            data.Add(node);
        }

        // do ways.
        var nodesInBox = new HashSet<long>(data.Select(x => x.Id.Value));
        var waysInSnapshot = await db.GetSnapshotWaysForNodesAsync(commit.Id, nodesInBox);
        var ways = new List<OsmGeo>();
        bool HasNodeInBox(Way way)
        {
            return way.Nodes is { Length: > 0 } && nodesInBox.Overlaps(way.Nodes);
        }

        var notInBox = new HashSet<long>();

        void LogNodesNotInBox(Way way)
        {
            if (way.Nodes is not { Length: > 0 }) return;

            notInBox?.UnionWith(way.Nodes
                .Where(x => !nodesInBox.Contains(x)));
        }

        foreach (var way in waysInSnapshot)
        {
            // check for changes, if not just add the way to the result set.
            LogNodesNotInBox(way);
            ways.Add(way);
        }

        // extra nodes not in box but part of way.
        var nodesNotInBox = await db.GetSnapshotNodesAsync(commit.Id, notInBox);
        data.AddRange(nodesNotInBox);
        data.AddRange(ways);

        // do relations.
        var members = new HashSet<OsmGeoKey>();
        members.UnionWith(nodesInBox.Select(x => new OsmGeoKey(OsmGeoType.Node, x)));
        members.UnionWith(ways.Select(x => new OsmGeoKey(x)));

        var relations = new List<OsmGeo>();
        var relationsInSnapshot = await db.GetSnapshotRelationsForMembersAsync(commit.Id, members);
        foreach (var relation in relationsInSnapshot)
        {
            relations.Add(relation);
        }

        data.AddRange(relations);

        bool HasMemberInBox(Relation relation)
        {
            return members != null && members.Overlaps(relation.Members?.Select(x => new OsmGeoKey(x.Type, x.Id))
                                                       ?? Array.Empty<OsmGeoKey>());
        }

        data.Sort((x, y) => x.CompareByIdVersionAndType(y));

        return data;
    }

    /// <summary>
    /// Gets the nodes with the given ids.
    /// </summary>
    /// <param name="db">The db.</param>
    /// <param name="snapshotCommitId">The commit id.</param>
    /// <param name="ids">The ids.</param>
    /// <returns>The nodes.</returns>
    public static async Task<IEnumerable<Node>> GetSnapshotNodesAsync(this OsmDbContext db, long snapshotCommitId,
        HashSet<long> ids)
    {
        var commitIds = await db.GetSnapshotCommitsInCommit(snapshotCommitId);

        var nodes = await db.SnapshotNodes
            .Where(x => commitIds.Contains(x.CommitId) &&
                        ids.Contains(x.Id))
            .OrderBy(x => x.Id).ThenBy(x => x.Version)
            .ToListAsync();

        IEnumerable<Node> GetLatestVersions(IEnumerable<SnapshotNode> nodes)
        {
            SnapshotNode? previous = null;
            foreach (var node in nodes)
            {
                if (previous == null)
                {
                    previous = node;
                    continue;
                }

                if (previous.Id == node.Id)
                {
                    previous = node;
                    continue;
                }

                var previousOsmGeo = previous.ToNode();
                if (previousOsmGeo != null) yield return previousOsmGeo;
                previous = node;
            }

            if (previous != null)
            {
                var previousOsmGeo = previous.ToNode();
                if (previousOsmGeo != null) yield return previousOsmGeo;
            }
        }

        return GetLatestVersions(nodes);
    }

    /// <summary>
    /// Gets the ways with the at least one node in the given set.
    /// </summary>
    /// <param name="db">The db.</param>
    /// <param name="snapshotCommitId">The commit.</param>
    /// <param name="ids">The node ids.</param>
    /// <returns>The ways.</returns>
    public static async Task<IEnumerable<Way>> GetSnapshotWaysForNodesAsync(this OsmDbContext db,
        long snapshotCommitId, HashSet<long> ids)
    {
        var commitIds = await db.GetSnapshotCommitsInCommit(snapshotCommitId);

        var wayIds = await db.SnapshotWayNodes
            .Where(x =>
                ids.Contains(x.Node) &&
                commitIds.Contains(x.SnapshotWayCommitId))
            .Select(x => x.SnapshotWayId).Distinct().ToListAsync();

        var waysWithNodes = await db.SnapshotWays
            .Include(w => w.SnapshotWayNodes)
            .Where(x =>
                wayIds.Contains(x.Id) &&
                commitIds.Contains(x.CommitId)).ToListAsync();

        waysWithNodes.Sort((w1, w2) =>
        {
            if (w1.Id < w2.Id)
            {
                return -1;
            }

            if (w1.Id > w2.Id)
            {
                return 1;
            }

            if (w1.Version < w2.Version)
            {
                return -1;
            }

            if (w1.Version > w2.Version)
            {
                return 1;
            }

            return 0;
        });

        IEnumerable<Way> GetLatestVersions(List<SnapshotWay> waysWithNodes)
        {
            SnapshotWay? previous = null;

            // loop over all ways and only return the latest versions.
            foreach (var way in waysWithNodes)
            {
                if (previous == null)
                {
                    previous = way;
                    continue;
                }

                if (previous.Id == way.Id)
                {
                    previous = way;
                    continue;
                }

                var previousOsmGeo = previous.ToWay();
                if (previousOsmGeo != null) yield return previousOsmGeo;
                previous = way;
            }

            if (previous != null)
            {
                var previousOsmGeo = previous.ToWay();
                if (previousOsmGeo != null) yield return previousOsmGeo;
            }
        }

        return GetLatestVersions(waysWithNodes);
    }


    /// <summary>
    /// Gets the relations with the at least one member in the given set.
    /// </summary>
    /// <param name="db">The db.</param>
    /// <param name="snapshotCommitId">The commit.</param>
    /// <param name="members">The member ids and types.</param>
    /// <returns>The ways.</returns>
    public static async Task<IEnumerable<Relation>> GetSnapshotRelationsForMembersAsync(this OsmDbContext db,
        long snapshotCommitId, HashSet<OsmGeoKey> members)
    {
        var commitIds = await db.GetSnapshotCommitsInCommit(snapshotCommitId);

        var nodeMemberIds = new HashSet<long>(members
            .Where(x => x.Type == OsmGeoType.Node).Select(x => x.Id));
        var wayMemberIds = new HashSet<long>(members
            .Where(x => x.Type == OsmGeoType.Way).Select(x => x.Id));
        var relationMemberIds = new HashSet<long>(members
            .Where(x => x.Type == OsmGeoType.Relation).Select(x => x.Id));

        var relationIds = await db.SnapshotRelationMembers
            .Where(rm => commitIds.Contains(rm.SnapshotRelationCommitId) &&
                         ((rm.MemberType == OsmGeoType.Node && nodeMemberIds.Contains(rm.MemberId)) ||
                          (rm.MemberType == OsmGeoType.Way && wayMemberIds.Contains(rm.MemberId)) ||
                          (rm.MemberType == OsmGeoType.Relation && relationMemberIds.Contains(rm.MemberId))))
            .Select(x => x.SnapshotRelationId).Distinct().ToListAsync();

        var relations = await db.SnapshotRelations
            .Include(w => w.Members)
            .Where(x =>
                relationIds.Contains(x.Id) &&
                commitIds.Contains(x.CommitId)).ToListAsync();

        relations.Sort((w1, w2) =>
        {
            if (w1.Id < w2.Id)
            {
                return -1;
            }

            if (w1.Id > w2.Id)
            {
                return 1;
            }

            if (w1.Version < w2.Version)
            {
                return -1;
            }

            if (w1.Version > w2.Version)
            {
                return 1;
            }

            return 0;
        });

        IEnumerable<Relation> GetLatestVersions(IEnumerable<SnapshotRelation> relations)
        {
            SnapshotRelation? previous = null;
            foreach (var relation in relations)
            {
                if (previous == null)
                {
                    previous = relation;
                    continue;
                }

                if (previous.Id == relation?.Id)
                {
                    previous = relation;
                    continue;
                }

                var previousOsmGeo = previous.ToRelation();
                if (previousOsmGeo != null) yield return previousOsmGeo;
                previous = relation;
            }

            if (previous != null)
            {
                var previousOsmGeo = previous.ToRelation();
                if (previousOsmGeo != null) yield return previousOsmGeo;
            }
        }

        return GetLatestVersions(relations);
    }

    /// <summary>
    /// Gets all the nodes in the given box.
    /// </summary>
    /// <param name="db">The db.</param>
    /// <param name="snapshotCommitId">The commit.</param>
    /// <param name="left">The left.</param>
    /// <param name="bottom">The bottom.</param>
    /// <param name="right">The right.</param>
    /// <param name="top">The top.</param>
    /// <returns>The nodes in the box.</returns>
    internal static async Task<IEnumerable<Node>> GetSnapshotNodesInBoxAsync(this OsmDbContext db,
        long snapshotCommitId,
        double left, double bottom, double right, double top, HashSet<long> tileIds)
    {
        var commitIds = await db.GetSnapshotCommitsInCommit(snapshotCommitId);

        var nodes = await db.SnapshotNodes
            .Where(x =>
                commitIds.Contains(x.CommitId) &&
                tileIds.Contains(x.TileId) &&
                x.Latitude >= bottom && x.Latitude < top && x.Longitude >= left && x.Longitude < right).ToListAsync();

        nodes.Sort((w1, w2) =>
        {
            if (w1.Id < w2.Id)
            {
                return -1;
            }

            if (w1.Id > w2.Id)
            {
                return 1;
            }

            if (w1.Version < w2.Version)
            {
                return -1;
            }

            if (w1.Version > w2.Version)
            {
                return 1;
            }

            return 0;
        });

        IEnumerable<Node> GetLatestVersions(List<SnapshotNode> nodes)
        {
            SnapshotNode? previous = null;
            foreach (var node in nodes)
            {
                if (previous == null)
                {
                    previous = node;
                    continue;
                }

                if (previous.Id == node.Id)
                {
                    previous = node;
                    continue;
                }

                var previousOsmGeo = previous.ToNode();
                if (previousOsmGeo != null) yield return previousOsmGeo;
                previous = node;
            }

            if (previous != null)
            {
                var previousOsmGeo = previous.ToNode();
                if (previousOsmGeo != null) yield return previousOsmGeo;
            }
        }

        return GetLatestVersions(nodes);
    }
    
    /// <summary>
    /// Gets the commit, before or at the given commit, with the latest data for the given tile.
    /// </summary>
    /// <param name="db">The db.</param>
    /// <param name="commit">The commit.</param>
    /// <param name="tileId">The tile.</param>
    /// <returns>The commit id.</returns>
    public static async Task<SnapshotCommit> GetLatestSnapshotCommitForTile(this OsmDbContext db,
        SnapshotCommit commit, long tileId)
    {
        var commits = await db.GetSnapshotCommitsInCommit(commit);
        if (commits.Count == 0) throw new InvalidDataException("at least one commit expected");

        // iterate over commit from recent to old.
        for (var i = 0; i < commits.Count; i++)
        {
            var current = commits[i];
            var allTiles = db.GetSnapshotCommitTilesCached(current);

            // no tiles are assumed to mean all tiles.
            if (allTiles.Count == 0)
                return await db.SnapshotCommits.FindAsync(current) ??
                       throw new InvalidDataException($"snapshot commit {current} not found");

            // check if the tile has changed in this commit.
            if (allTiles.Contains(tileId))
                return await db.SnapshotCommits.FindAsync(current) ??
                       throw new InvalidDataException($"snapshot commit {current} not found");
        }

        // nothing was found, return oldest, the tile is not in any snapshot commit.
        return await db.SnapshotCommits.FindAsync(commits[0]) ??
               throw new InvalidDataException($"snapshot commit {commits[0]} not found");
    }
    
    private static readonly ConcurrentDictionary<long, ImmutableHashSet<long>> SnapshotCommitTiles = new();

    /// <summary>
    /// Gets the modified tiles in the given commit.
    /// </summary>
    /// <param name="db">The db.</param>
    /// <param name="commitId">The commit id.</param>
    /// <returns>A hashset with all modified tiles.</returns>
    private static ImmutableHashSet<long> GetSnapshotCommitTilesCached(this OsmDbContext db, long commitId)
    {
        return SnapshotCommitTiles.GetOrAdd(commitId, (_) =>
        {
            return db.SnapshotCommitTiles
                .Where(x => x.CommitId == commitId)
                .Select(x => x.TileId).ToImmutableHashSet();
        });
    }

    /// <summary>
    /// Get all the commits before and including the given commit.
    /// </summary>
    /// <param name="db">The db context.</param>
    /// <param name="commit">The commit.</param>
    /// <returns>All ids of the commits that come before the given commit from the latest at the lowest index to oldest at higher indexes.</returns>
    internal static async Task<List<long>> GetSnapshotCommitsInCommit(this OsmDbContext db, SnapshotCommit commit)
    {
        var cacheResult = db.SnapshotCommitIdsCache.TryFetchFromCache(commit.Id);
        if (cacheResult.success) return cacheResult.commitIdsBefore;

        var commitIdsBefore = await
            db.SnapshotCommits.Where(x => x.SnapshotId == commit.SnapshotId
                                          && x.Sequence <= commit.Sequence)
                .OrderByDescending(x => x.Sequence)
                .Select(x => x.Id)
                .ToListAsync();

        db.SnapshotCommitIdsCache.Set(commit.Id, commitIdsBefore);
        return commitIdsBefore;
    }

    /// <summary>
    /// Get all the commits before and including the given commit.
    /// </summary>
    /// <param name="db">The db context.</param>
    /// <param name="snapshotCommitId">The snapshot commit id.</param>
    /// <returns>All ids of the commits that come before the given commit from the latest at the lowest index to oldest at higher indexes.</returns>
    internal static async Task<List<long>> GetSnapshotCommitsInCommit(this OsmDbContext db, long snapshotCommitId)
    {
        var cacheResult = db.SnapshotCommitIdsCache.TryFetchFromCache(snapshotCommitId);
        if (cacheResult.success) return cacheResult.commitIdsBefore;

        var commit = await db.SnapshotCommits.FindAsync(snapshotCommitId);
        if (commit == null) throw new InvalidDataException($"Snapshot commit {snapshotCommitId} not found");
        var commitIdsBefore = await
            db.SnapshotCommits.Where(x => x.SnapshotId == commit.SnapshotId
                                          && x.Sequence <= commit.Sequence)
                .OrderByDescending(x => x.Sequence)
                .Select(x => x.Id)
                .ToListAsync();

        db.SnapshotCommitIdsCache.Set(commit.Id, commitIdsBefore);
        return commitIdsBefore;
    }
}