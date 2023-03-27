namespace RoutableTiles.API.Db.Tables;

public class SnapshotWayNodes
{
    /// <summary>
    /// The way id.
    /// </summary>
    public long SnapshotWayId { get; set; }

    /// <summary>
    /// The commit id.
    /// </summary>
    public long SnapshotWayCommitId { get; set; }

    /// <summary>
    /// The commit.
    /// </summary>
    public SnapshotCommit SnapshotWayCommit { get; set; }

    /// <summary>
    /// The version.
    /// </summary>
    public int SnapshotWayVersion { get; set; }

    /// <summary>
    /// The snapshot way.
    /// </summary>
    public SnapshotWay Way { get; set; }

    /// <summary>
    /// The node index.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// The node id.
    /// </summary>
    public long Node { get; set; }
}
