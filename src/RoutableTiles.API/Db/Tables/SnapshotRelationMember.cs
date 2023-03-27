using OsmSharp;

namespace RoutableTiles.API.Db.Tables;

/// <summary>
/// A relation member.
/// </summary>
public class SnapshotRelationMember
{
    /// <summary>
    /// The relation id.
    /// </summary>
    public long SnapshotRelationId { get; set; }

    /// <summary>
    /// The relation version.
    /// </summary>
    public int SnapshotRelationVersion { get; set; }

    /// <summary>
    /// The commit id.
    /// </summary>
    public long SnapshotRelationCommitId { get; set; }

    /// <summary>
    /// The relation.
    /// </summary>
    public SnapshotRelation? SnapshotRelation { get; set; }

    /// <summary>
    /// The order.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// The member type.
    /// </summary>
    public OsmGeoType MemberType { get; set; }

    /// <summary>
    /// The member id.
    /// </summary>
    public long MemberId { get; set; }

    /// <summary>
    /// The member role.
    /// </summary>
    public string? MemberRole { get; set; }
}
