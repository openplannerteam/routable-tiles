using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using OsmSharp;
using OsmSharp.Tags;
using RouteableTiles.IO.JsonLD.Semantics;
using RouteableTiles.Tiles;

[assembly: InternalsVisibleTo("RouteableTiles.Tests")]
[assembly: InternalsVisibleTo("RouteableTiles.Tests.Functional")]
namespace RouteableTiles.IO.JsonLD
{
    public static class JsonSerializer
    {
//        /// <summary>
//        /// The semantic mapping to use.
//        /// </summary>
//        public static readonly Dictionary<string, TagMapperConfig> SemanticMapping = new Dictionary<string, TagMapperConfig>()
//        {
//            {
//                "highway", new TagMapperConfig()
//                {
//                    osm_key = "highway",
//                    predicate = "osm:highway",
//                    mapping = new Dictionary<string, object>()
//                    {
//                        {"motorway", "osm:Motorway"},
//                        {"trunk", "osm:Trunk"},
//                        {"primary", "osm:Primary"},
//                        {"secondary", "osm:Secondary"},
//                        {"tertiary", "osm:Tertiary"},
//                        {"unclassified", "osm:Unclassified"},
//                        {"residential", "osm:Residential"},
//                        {"motorway_link", "osm:MotorwayLink"},
//                        {"trunk_link", "osm:TrunkLink"},
//                        {"primary_link", "osm:PrimaryLink"},
//                        {"secondary_link", "osm:SecondaryLink"},
//                        {"tertiary_link", "osm:TertiaryLink"},
//                        {"service", "osm:Service"},
//                        {"track", "osm:Track"},
//                        {"footway", "osm:Footway"},
//                        {"path", "osm:Path"},
//                        {"living_street", "osm:LivingStreet"},
//                        {"cycleway", "osm:Cycleway"}
//                    }
//                }
//            },
//            {
//                "name", new TagMapperConfig()
//                {
//                    osm_key = "name",
//                    predicate = "osm:name"
//                }
//            }
//        };
        
        /// <summary>
        /// Writes the given enumerable of osm geo objects to the given text writer in JSON-LD routeable tiles format.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="tile">The tile.</param>
        /// <param name="writer">The writer.</param>
        /// <param name="mapping">The mapping.</param>
        public static void WriteTo(this IEnumerable<OsmGeo> data, TextWriter writer, Tile tile, Dictionary<string, TagMapperConfig> mapping)
        {
            var jsonWriter = new JsonWriter(writer);
            jsonWriter.WriteOpen();
            
            jsonWriter.WriteContext(tile, mapping);
            
            jsonWriter.WritePropertyName("@graph");
            jsonWriter.WriteArrayOpen();

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
            
            jsonWriter.WriteArrayClose();
            jsonWriter.WriteClose();
            jsonWriter.Flush();
        }
        
        internal static void WriteContext(this JsonWriter writer, Tile tile, Dictionary<string, TagMapperConfig> mapping)
        {
            writer.WritePropertyName("@context");
            writer.WriteOpen();
            
            writer.WriteProperty("tiles", "https://w3id.org/tree/terms#", true);
            writer.WriteProperty("hydra", "http://www.w3.org/ns/hydra/core#", true);
            writer.WriteProperty("osm", "https://w3id.org/openstreetmap/terms#", true);
            writer.WriteProperty("rdfs", "http://www.w3.org/2000/01/rdf-schema#", true);
            writer.WriteProperty("geo", "http://www.w3.org/2003/01/geo/wgs84_pos#", true);
            writer.WriteProperty("dcterms", "http://purl.org/dc/terms/", true);
            writer.WritePropertyName("dcterms:license");
            writer.WriteOpen();
            writer.WriteProperty("@type", "@id", true);
            writer.WriteClose();
            writer.WritePropertyName("hydra:variableRepresentation", true);
            writer.WriteOpen();
            writer.WriteProperty("@type", "@id", true);
            writer.WriteClose();
            writer.WritePropertyName("hydra:property");
            writer.WriteOpen();
            writer.WriteProperty("@type", "@id", true);
            writer.WriteClose();

            foreach (var map in mapping)
            {
                if (map.Value.mapping == null || map.Value.mapping.Count == 0) continue;
                
                writer.WritePropertyName(map.Value.predicate);
                writer.WriteOpen();
                writer.WriteProperty("@type", "@id", true);
                writer.WriteClose();
            }
            
            writer.WritePropertyName("osm:hasNodes");
            writer.WriteOpen();
            writer.WriteProperty("@container", "@list", true);
            writer.WriteProperty("@type", "@id", true);
            writer.WriteClose();
            
            writer.WritePropertyName("osm:hasMembers");
            writer.WriteOpen();
            writer.WriteProperty("@container", "@list", true);
            writer.WriteProperty("@type", "@id", true);
            writer.WriteClose();
            
            writer.WriteClose();
            
            writer.WriteProperty("@id", $"https://tiles.openplanner.team/planet/{tile.Zoom}/{tile.X}/{tile.Y}/", true);
            writer.WriteProperty("tiles:zoom", $"{tile.Zoom}");
            writer.WriteProperty("tiles:longitudeTile", $"{tile.X}");
            writer.WriteProperty("tiles:latitudeTile", $"{tile.Y}");
            
            writer.WritePropertyName("dcterms:isPartOf");
            writer.WriteOpen();
            
            // TODO: generate this URL based on the request info instead of hardcoding, it's possible this is hosted somewhere else.
            writer.WriteProperty("@id", $"https://tiles.openplanner.team/planet/", true);
            writer.WriteProperty("@type", "hydra:Collection", true);
            writer.WriteProperty("dcterms:license", "http://opendatacommons.org/licenses/odbl/1-0/", true);
            writer.WriteProperty("dcterms:rights", "http://www.openstreetmap.org/copyright", true);
            
            writer.WritePropertyName("hydra:search");
            writer.WriteOpen();
            writer.WriteProperty("@type", "hydra:IriTemplate", true);
            writer.WriteProperty("hydra:template", "https://tiles.openplanner.team/planet/14/{x}/{y}", true);
            writer.WriteProperty("hydra:variableRepresentation", "hydra:BasicRepresentation", true);
            
            writer.WritePropertyName("hydra:mapping");
            writer.WriteArrayOpen();
            writer.WriteOpen();
            writer.WriteProperty("@type", "hydra:IriTemplateMapping", true);
            writer.WriteProperty("hydra:variable", "x", true);
            writer.WriteProperty("hydra:property", "tiles:longitudeTile", true);
            writer.WriteProperty("hydra:required", "true");
            writer.WriteClose();
            writer.WriteOpen();
            writer.WriteProperty("@type", "hydra:IriTemplateMapping", true);
            writer.WriteProperty("hydra:variable", "y", true);
            writer.WriteProperty("hydra:property", "tiles:latitudeTile", true);
            writer.WriteProperty("hydra:required", "true");
            writer.WriteClose();
            writer.WriteArrayClose();
            writer.WriteClose();
            
            writer.WriteClose();
        }

        internal static void WriteNode(this JsonWriter writer, Node node, Dictionary<string, TagMapperConfig> mapping)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            writer.WriteOpen();

            writer.WriteProperty("@id", $"http://www.openstreetmap.org/node/{node.Id}", true, false);
            writer.WriteProperty("geo:long", node.Longitude.ToInvariantString());
            writer.WriteProperty("geo:lat", node.Latitude.ToInvariantString());
            
            if (node.Tags != null)
            {
                writer.WriteTags(node.Tags, mapping);
            }

            writer.WriteClose();
        }

        internal static void WriteWay(this JsonWriter writer, Way way, Dictionary<string, TagMapperConfig> mapping)
        {
            if (writer == null) { throw new ArgumentNullException(nameof(writer)); }
            if (way == null) { throw new ArgumentNullException(nameof(way)); }
            
            writer.WriteOpen();
            
            writer.WriteProperty("@id", $"http://www.openstreetmap.org/way/{way.Id}", true, false);
            writer.WriteProperty("@type", "osm:Way", true, true);

            if (way.Tags != null)
            {
                writer.WriteTags(way.Tags, mapping);
            }
            
            writer.WritePropertyName("osm:hasNodes");
            
            writer.WriteArrayOpen();
            if (way.Nodes != null)
            {
                foreach (var node in way.Nodes)
                {
                    writer.WriteArrayValue($"http://www.openstreetmap.org/node/{node}", true, false);
                }
            }
            writer.WriteArrayClose();
            
            writer.WriteClose();
        }

        internal static void WriteRelation(this JsonWriter writer, Relation relation, Dictionary<string, TagMapperConfig> mapping)
        {
            if (writer == null) { throw new ArgumentNullException(nameof(writer)); }
            if (relation == null) { throw new ArgumentNullException(nameof(relation)); }
            
            writer.WriteOpen();
            
            writer.WriteProperty("@id", $"http://www.openstreetmap.org/relation/{relation.Id}", true, false);
            writer.WriteProperty("@type", "osm:Relation", true, true);

            if (relation.Tags != null)
            {
                writer.WriteTags(relation.Tags, mapping);
            }
            
            writer.WritePropertyName("osm:hasMembers");
            
            writer.WriteArrayOpen();
            if (relation.Members != null)
            {
                foreach (var member in relation.Members)
                {
                    writer.WriteOpen();

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
                    
                    writer.WriteClose();
                }
            }
            writer.WriteArrayClose();
            
            writer.WriteClose();
        }

        internal static void WriteTags(this JsonWriter writer, TagsCollectionBase tags, Dictionary<string, TagMapperConfig> mapping)
        {
            var undefinedTags = new List<Tag>();
            foreach (var tag in tags)
            {
                if (tag.Map(mapping, writer)) continue;
                
                undefinedTags.Add(tag);
            }
            
            writer.WritePropertyName("osm:hasTag");
            writer.WriteArrayOpen();
            foreach (var tag in undefinedTags)
            {
                writer.WriteArrayValue($"{tag.Key}={tag.Value}", true, true);
            }
            writer.WriteArrayClose();
        }
    }
}
