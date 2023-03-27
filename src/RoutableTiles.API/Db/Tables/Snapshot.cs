namespace RoutableTiles.API.Db.Tables;

/// <summary>
/// Represents a snapshot of OSM data that can be updated using snapshot commits.
/// </summary>
public class Snapshot
{
    /// <summary>
    /// The polygon extent key, stores a geojson polygon as a feature.
    /// </summary>
    public const string PolygonExtentKey = "extent_polygon";

    /// <summary>
    /// The id.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// The global id.
    /// </summary>
    public Guid GlobalId { get; set; }

    /// <summary>
    /// Gets or sets the tags.
    /// </summary>
    public Dictionary<string, string> Tags { get; set; } = null!;

    /// <summary>
    /// The name of this commit.
    /// </summary>
    /// <remarks>
    /// Ex: Belgium planet file.
    /// </remarks>
    public string? Name { get; set; }

    /// <summary>
    /// The description of this commit.
    /// </summary>
    /// <remarks>
    /// Ex: Belgium planet file started at 2021-02-20T14:00:00Z.
    /// </remarks>
    public string? Description { get; set; }

    /// <summary>
    /// The commits in this branch.
    /// </summary>
    public List<SnapshotCommit> Commits { get; set; }
}
