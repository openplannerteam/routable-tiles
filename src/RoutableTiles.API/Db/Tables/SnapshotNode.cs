using System.Collections.Generic;

namespace RoutableTiles.API.Db.Tables;


/// <summary>
/// A node as part of a snapshot.
/// </summary>
public class SnapshotNode
{
    /// <summary>
    /// The zoom level for the tile id.
    /// </summary>
    public const int TileIdZoomLevel = 14;

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
    /// The latitude.
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// The longitude.
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// The tile id, include for performance reasons.
    /// </summary>
    public long TileId { get; set; }
}
