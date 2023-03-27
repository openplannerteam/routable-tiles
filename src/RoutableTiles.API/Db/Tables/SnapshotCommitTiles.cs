namespace RoutableTiles.API.Db.Tables;

/// <summary>
/// Represents a changed tile in the commit.
/// </summary>
public class SnapshotCommitTiles
{
    /// <summary>
    /// The id.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// The commit id.
    /// </summary>
    public long CommitId { get; set; }

    /// <summary>
    /// The commit.
    /// </summary>
    public SnapshotCommit? Commit { get; set; }

    /// <summary>
    /// The tile id.
    /// </summary>
    public long TileId { get; set; }
}
