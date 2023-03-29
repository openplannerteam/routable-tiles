using System.Text.Json;
using OsmSharp;
using OsmSharp.Tags;
using RoutableTiles.API.Controllers.Formatters.JsonLd.Semantics;
using RoutableTiles.API.Db.Tiles;

namespace RoutableTiles.API.Controllers.Formatters.JsonLd;

internal static class JsonLdTileSerializer
{
    /// <summary>
    /// Writes the given enumerable of osm geo objects to the given text writer in JSON-LD routable tiles format.
    /// </summary>
    /// <param name="data">The data.</param>
    /// <param name="tile">The tile.</param>
    /// <param name="baseUrl">The base url.</param>
    /// <param name="jsonWriter">The writer.</param>
    /// <param name="mapping">The mapping.</param>
    public static async Task WriteTo(this IEnumerable<OsmGeo> data, Utf8JsonWriter jsonWriter, Tile tile,
        string baseUrl, Dictionary<string, TagMapperConfig> mapping)
    {
        if (!baseUrl.EndsWith("/")) baseUrl += '/';

        jsonWriter.WriteStartObject();

        jsonWriter.WriteContext(tile, baseUrl, mapping);

        jsonWriter.WritePropertyName("@graph");
        jsonWriter.WriteStartArray();

        foreach (var osmGeo in data)
        {
            switch (osmGeo)
            {
                case Node node:
                    jsonWriter.WriteNode(node, mapping);
                    break;
                case Way way:
                    jsonWriter.WriteWay(way, mapping);
                    break;
                case Relation relation:
                    jsonWriter.WriteRelation(relation, mapping);
                    break;
            }
        }

        jsonWriter.WriteEndArray();
        jsonWriter.WriteEndObject();
        await jsonWriter.FlushAsync();
    }

    internal static void WriteContext(this Utf8JsonWriter writer, Tile tile, string baseUrl,
        Dictionary<string, TagMapperConfig> mapping)
    {
        writer.WritePropertyName("@context");
        writer.WriteStartObject();

        writer.WriteProperty("tiles", "https://w3id.org/tree/terms#", true);
        writer.WriteProperty("hydra", "http://www.w3.org/ns/hydra/core#", true);
        writer.WriteProperty("osm", "https://w3id.org/openstreetmap/terms#", true);
        writer.WriteProperty("rdfs", "http://www.w3.org/2000/01/rdf-schema#", true);
        writer.WriteProperty("geo", "http://www.w3.org/2003/01/geo/wgs84_pos#", true);
        writer.WriteProperty("dcterms", "http://purl.org/dc/terms/", true);
        writer.WritePropertyName("dcterms:license");
        writer.WriteStartObject();
        writer.WriteProperty("@type", "@id", true);
        writer.WriteEndObject();
        writer.WritePropertyName("hydra:variableRepresentation");
        writer.WriteStartObject();
        writer.WriteProperty("@type", "@id", true);
        writer.WriteEndObject();
        writer.WritePropertyName("hydra:property");
        writer.WriteStartObject();
        writer.WriteProperty("@type", "@id", true);
        writer.WriteEndObject();

        foreach (var map in mapping)
        {
            if (map.Value.mapping?.Count == 0) continue;

            writer.WritePropertyName(map.Value.predicate);
            writer.WriteStartObject();
            writer.WriteProperty("@type", "@id", true);
            writer.WriteEndObject();
        }

        writer.WritePropertyName("osm:hasNodes");
        writer.WriteStartObject();
        writer.WriteProperty("@container", "@list", true);
        writer.WriteProperty("@type", "@id", true);
        writer.WriteEndObject();

        writer.WritePropertyName("osm:hasMembers");
        writer.WriteStartObject();
        writer.WriteProperty("@container", "@list", true);
        writer.WriteProperty("@type", "@id", true);
        writer.WriteEndObject();

        writer.WriteEndObject();

        writer.WriteProperty("@id", $"{baseUrl}{tile.Zoom}/{tile.X}/{tile.Y}/", true);
        writer.WriteProperty("tiles:zoom", $"{tile.Zoom}");
        writer.WriteProperty("tiles:longitudeTile", $"{tile.X}");
        writer.WriteProperty("tiles:latitudeTile", $"{tile.Y}");

        writer.WritePropertyName("dcterms:isPartOf");
        writer.WriteStartObject();

        writer.WriteProperty("@id", baseUrl, true);
        writer.WriteProperty("@type", "hydra:Collection", true);
        writer.WriteProperty("dcterms:license", "http://opendatacommons.org/licenses/odbl/1-0/", true);
        writer.WriteProperty("dcterms:rights", "http://www.openstreetmap.org/copyright", true);

        writer.WritePropertyName("hydra:search");
        writer.WriteStartObject();
        writer.WriteProperty("@type", "hydra:IriTemplate", true);
        writer.WriteProperty("hydra:template", $"{baseUrl}" + "14/{x}/{y}", true);
        writer.WriteProperty("hydra:variableRepresentation", "hydra:BasicRepresentation", true);

        writer.WritePropertyName("hydra:mapping");
        writer.WriteStartArray();
        writer.WriteStartObject();
        writer.WriteProperty("@type", "hydra:IriTemplateMapping", true);
        writer.WriteProperty("hydra:variable", "x", true);
        writer.WriteProperty("hydra:property", "tiles:longitudeTile", true);
        writer.WriteProperty("hydra:required", "true");
        writer.WriteEndObject();
        writer.WriteStartObject();
        writer.WriteProperty("@type", "hydra:IriTemplateMapping", true);
        writer.WriteProperty("hydra:variable", "y", true);
        writer.WriteProperty("hydra:property", "tiles:latitudeTile", true);
        writer.WriteProperty("hydra:required", "true");
        writer.WriteEndObject();
        writer.WriteEndArray();
        writer.WriteEndObject();

        writer.WriteEndObject();
    }

    internal static void WriteNode(this Utf8JsonWriter writer, Node node, Dictionary<string, TagMapperConfig> mapping)
    {
        if (writer == null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        writer.WriteStartObject();

        writer.WriteProperty("@id", $"http://www.openstreetmap.org/node/{node.Id}", true, false);
        writer.WriteProperty("@type", "osm:Node", true, true);
        writer.WriteProperty("geo:long", node.Longitude.ToInvariantString());
        writer.WriteProperty("geo:lat", node.Latitude.ToInvariantString());

        if (node.Tags != null)
        {
            writer.WriteTags(node.Tags, mapping);
        }

        writer.WriteEndObject();
    }

    internal static void WriteWay(this Utf8JsonWriter writer, Way way, Dictionary<string, TagMapperConfig> mapping)
    {
        if (writer == null) { throw new ArgumentNullException(nameof(writer)); }
        if (way == null) { throw new ArgumentNullException(nameof(way)); }

        writer.WriteStartObject();

        writer.WriteProperty("@id", $"http://www.openstreetmap.org/way/{way.Id}", true, false);
        writer.WriteProperty("@type", "osm:Way", true, true);

        if (way.Tags != null)
        {
            writer.WriteTags(way.Tags, mapping);
        }

        writer.WritePropertyName("osm:hasNodes");

        writer.WriteStartArray();
        if (way.Nodes != null)
        {
            foreach (var node in way.Nodes)
            {
                writer.WriteStringValue($"http://www.openstreetmap.org/node/{node}");
            }
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
    }

    internal static void WriteRelation(this Utf8JsonWriter writer, Relation relation, Dictionary<string, TagMapperConfig> mapping)
    {
        if (writer == null) { throw new ArgumentNullException(nameof(writer)); }
        if (relation == null) { throw new ArgumentNullException(nameof(relation)); }

        writer.WriteStartObject();

        writer.WriteProperty("@id", $"http://www.openstreetmap.org/relation/{relation.Id}", true, false);
        writer.WriteProperty("@type", "osm:Relation", true, true);

        if (relation.Tags != null)
        {
            writer.WriteTags(relation.Tags, mapping);
        }

        writer.WritePropertyName("osm:hasMembers");

        writer.WriteStartArray();
        if (relation.Members != null)
        {
            foreach (var member in relation.Members)
            {
                writer.WriteStartObject();

                switch (member.Type)
                {
                    case OsmGeoType.Node:
                        writer.WriteProperty("@id", $"http://www.openstreetmap.org/node/{member.Id}", true, false);
                        break;
                    case OsmGeoType.Way:
                        writer.WriteProperty("@id", $"http://www.openstreetmap.org/way/{member.Id}", true, false);
                        break;
                    case OsmGeoType.Relation:
                        writer.WriteProperty("@id", $"http://www.openstreetmap.org/relation/{member.Id}", true, false);
                        break;
                }

                if (!string.IsNullOrWhiteSpace(member.Role))
                {
                    writer.WriteProperty("role", member.Role, true, false);
                }

                writer.WriteEndObject();
            }
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
    }

    internal static void WriteTags(this Utf8JsonWriter writer, TagsCollectionBase tags, Dictionary<string, TagMapperConfig> mapping)
    {
        var undefinedTags = new List<Tag>();
        foreach (var tag in tags)
        {
            if (tag.Map(mapping, writer)) continue;

            undefinedTags.Add(tag);
        }

        writer.WritePropertyName("osm:hasTag");
        writer.WriteStartArray();
        foreach (var tag in undefinedTags)
        {
            writer.WriteStringValue($"{tag.Key}={tag.Value}");
        }
        writer.WriteEndArray();
    }

    /// <summary>
    /// Writes a property and it's value.
    /// </summary>
    internal static void WriteProperty(this Utf8JsonWriter writer, string name, string value, bool useQuotes = false, bool escape = false)
    {
        if (escape) name = JsonTools.Escape(name);
        if (escape) value = JsonTools.Escape(value);

        writer.WritePropertyName(name);
        if (useQuotes)
        {
            writer.WriteStringValue(value);
        }
        else
        {
            writer.WriteRawValue(value);
        }
    }
}
