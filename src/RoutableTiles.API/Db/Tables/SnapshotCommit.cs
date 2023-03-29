namespace RoutableTiles.API.Db.Tables;

/// <summary>
/// Represents a single immutable consistent snapshot of OSM daa.
/// </summary>
public class SnapshotCommit
{
    /// <summary>
    /// The id.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// The global id.
    /// </summary>
    public Guid GlobalId { get; set; }

    /// <summary>
    /// The id of the snapshot.
    /// </summary>
    public long SnapshotId { get; set; }

    /// <summary>
    /// The snapshot.
    /// </summary>
    public Snapshot? Snapshot { get; set; }

    /// <summary>
    /// Tags to add meta-data.
    /// </summary>
    public Dictionary<string, string> Tags { get; set; } = null!;

    /// <summary>
    /// The sequence number, increases with each new commit within one snapshot.
    /// </summary>
    public long Sequence { get; set; }

    /// <summary>
    /// The timestamp of when the data was committed.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// When true the data has been committed and won't change anymore.
    /// </summary>
    public bool Committed { get; set; }

    /// <summary>
    /// The name of this commit.
    /// </summary>
    /// <remarks>
    /// Ex: Diff until 2021-02-20T14:00:00Z.
    /// </remarks>
    public string? Name { get; set; }

    /// <summary>
    /// The description of this commit.
    /// </summary>
    /// <remarks>
    /// Ex: Diff applied to belgium-latest.osm.pbf, up until 2021-02-20T14:00:00Z.
    /// </remarks>
    public string? Description { get; set; }
}
