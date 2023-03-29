using OsmSharp;
using OsmSharp.Tags;
using RoutableTiles.API.Db.Tables;

namespace RoutableTiles.API.Db;

public static class OsmExtensions
{
    public static Node? ToNode(this SnapshotNode node)
    {
        if (node.Deleted) return null;

        return new Node()
        {
            Id = node.Id,
            Latitude = node.Latitude,
            Longitude = node.Longitude,
            Tags = node.Tags != null ? new TagsCollection(node.Tags.Select(x => new Tag(x.Key, x.Value))) : null,
            Version = (int)node.Version,
            TimeStamp = null,
            Visible = !node.Deleted,
            UserId = 0,
            UserName = string.Empty,
            ChangeSetId = 1
        };
    }

    public static Way? ToWay(this SnapshotWay way)
    {
        if (way.Deleted) return null;

        way.SnapshotWayNodes.Sort((x, y) => x.Index - y.Index);

        return new Way()
        {
            Id = way.Id,
            Nodes = way.SnapshotWayNodes.Select(x => x.Node).ToArray(),
            Tags = way.Tags != null ? new TagsCollection(way.Tags.Select(x => new Tag(x.Key, x.Value))) : null,
            Version = (int)way.Version,
            TimeStamp = null,
            Visible = !way.Deleted,
            UserId = 0,
            UserName = string.Empty,
            ChangeSetId = 1
        };
    }

    public static Relation? ToRelation(this SnapshotRelation relation)
    {
        if (relation.Deleted) return null;

        return new Relation()
        {
            Id = relation.Id,
            Members = relation.Members?.Select(x => new RelationMember()
            {
                Id = x.MemberId,
                Role = x.MemberRole,
                Type = x.MemberType
            }).ToArray(),
            Tags = relation.Tags != null ? new TagsCollection(relation.Tags.Select(x => new Tag(x.Key, x.Value))) : null,
            Version = (int)relation.Version,
            TimeStamp = null,
            Visible = !relation.Deleted,
            UserId = 0,
            UserName = string.Empty,
            ChangeSetId = 1
        };
    }
}
