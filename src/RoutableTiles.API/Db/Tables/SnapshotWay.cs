using System.Collections.Generic;

namespace RoutableTiles.API.Db.Tables;

/// <summary>
/// A way as part of a snapshot.
/// </summary>
public class SnapshotWay
{
    /// <summary>
    /// The id.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// The version.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// The deleted flag.
    /// </summary>
    public bool Deleted { get; set; }

    /// <summary>
    /// The commit id.
    /// </summary>
    public long CommitId { get; set; }

    /// <summary>
    /// The commit.
    /// </summary>
    public SnapshotCommit? Commit { get; set; }

    /// <summary>
    /// The tags.
    /// </summary>
    public Dictionary<string, string>? Tags { get; set; }

    /// <summary>
    /// The nodes.
    /// </summary>
    public List<SnapshotWayNodes> SnapshotWayNodes { get; set; }
}
