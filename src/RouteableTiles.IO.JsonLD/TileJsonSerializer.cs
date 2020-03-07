using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using OsmSharp;
using OsmSharp.Tags;
using RouteableTiles.IO.JsonLD.Semantics;
using RouteableTiles.IO.JsonLD.Tiles;

[assembly: InternalsVisibleTo("RouteableTiles.Tests")]
[assembly: InternalsVisibleTo("RouteableTiles.Tests.Functional")]
namespace RouteableTiles.IO.JsonLD
{
    public static class JsonSerializer
    {
        /// <summary>
        /// Writes the given enumerable of osm geo objects to the given text writer in JSON-LD routeable tiles format.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="tile">The tile.</param>
        /// <param name="baseUrl">The base url.</param>
        /// <param name="writer">The writer.</param>
        /// <param name="mapping">The mapping.</param>
        public static async Task WriteTo(this IEnumerable<OsmGeo> data, TextWriter writer, Tile tile, string baseUrl, Dictionary<string, TagMapperConfig> mapping)
        {
            var jsonWriter = new JsonWriter(writer);
            await jsonWriter.WriteOpenAsync();
            
            await jsonWriter.WriteContextAsync(tile, baseUrl, mapping);
            
            await jsonWriter.WritePropertyNameAsync("@graph");
            await jsonWriter.WriteArrayOpenAsync();

            foreach (var osmGeo in data)
            {
                switch (osmGeo)
                {
                    case Node node:
                        await jsonWriter.WriteNodeAsync(node, mapping);
                        break;
                    case Way way:
                        await jsonWriter.WriteWayAsync(way, mapping);
                        break;
                    case Relation relation:
                        await jsonWriter.WriteRelationAsync(relation, mapping);
                        break;
                }
            }
            
            await jsonWriter.WriteArrayCloseAsync();
            await jsonWriter.WriteCloseAsync();
            await jsonWriter.FlushAsync();
        }
        
        internal static async Task WriteContextAsync(this JsonWriter writer, Tile tile, string baseUrl, Dictionary<string, TagMapperConfig> mapping)
        {
            await writer.WritePropertyNameAsync("@context");
            await writer.WriteOpenAsync();
            
            await writer.WritePropertyAsync("tiles", "https://w3id.org/tree/terms#", true);
            await writer.WritePropertyAsync("hydra", "http://www.w3.org/ns/hydra/core#", true);
            await writer.WritePropertyAsync("osm", "https://w3id.org/openstreetmap/terms#", true);
            await writer.WritePropertyAsync("rdfs", "http://www.w3.org/2000/01/rdf-schema#", true);
            await writer.WritePropertyAsync("geo", "http://www.w3.org/2003/01/geo/wgs84_pos#", true);
            await writer.WritePropertyAsync("dcterms", "http://purl.org/dc/terms/", true);
            await writer.WritePropertyNameAsync("dcterms:license");
            await writer.WriteOpenAsync();
            await writer.WritePropertyAsync("@type", "@id", true);
            await writer.WriteCloseAsync();
            await writer.WritePropertyNameAsync("hydra:variableRepresentation", true);
            await writer.WriteOpenAsync();
            await writer.WritePropertyAsync("@type", "@id", true);
            await writer.WriteCloseAsync();
            await writer.WritePropertyNameAsync("hydra:property");
            await writer.WriteOpenAsync();
            await writer.WritePropertyAsync("@type", "@id", true);
            await writer.WriteCloseAsync();

            foreach (var map in mapping)
            {
                if (map.Value.mapping == null || map.Value.mapping.Count == 0) continue;
                
                await writer.WritePropertyNameAsync(map.Value.predicate);
                await writer.WriteOpenAsync();
                await writer.WritePropertyAsync("@type", "@id", true);
                await writer.WriteCloseAsync();
            }
            
            await writer.WritePropertyNameAsync("osm:hasNodes");
            await writer.WriteOpenAsync();
            await writer.WritePropertyAsync("@container", "@list", true);
            await writer.WritePropertyAsync("@type", "@id", true);
            await writer.WriteCloseAsync();
            
            await writer.WritePropertyNameAsync("osm:hasMembers");
            await writer.WriteOpenAsync();
            await writer.WritePropertyAsync("@container", "@list", true);
            await writer.WritePropertyAsync("@type", "@id", true);
            await writer.WriteCloseAsync();
            
            await writer.WriteCloseAsync();
            
            await writer.WritePropertyAsync("@id", $"{baseUrl}{tile.Zoom}/{tile.X}/{tile.Y}/", true);
            await writer.WritePropertyAsync("tiles:zoom", $"{tile.Zoom}");
            await writer.WritePropertyAsync("tiles:longitudeTile", $"{tile.X}");
            await writer.WritePropertyAsync("tiles:latitudeTile", $"{tile.Y}");
            
            await writer.WritePropertyNameAsync("dcterms:isPartOf");
            await writer.WriteOpenAsync();
            
            await writer.WritePropertyAsync("@id", baseUrl, true);
            await writer.WritePropertyAsync("@type", "hydra:Collection", true);
            await writer.WritePropertyAsync("dcterms:license", "http://opendatacommons.org/licenses/odbl/1-0/", true);
            await writer.WritePropertyAsync("dcterms:rights", "http://www.openstreetmap.org/copyright", true);
            
            await writer.WritePropertyNameAsync("hydra:search");
            await writer.WriteOpenAsync();
            await writer.WritePropertyAsync("@type", "hydra:IriTemplate", true);
            await writer.WritePropertyAsync("hydra:template", $"{baseUrl}" + "/14/{x}/{y}", true);
            await writer.WritePropertyAsync("hydra:variableRepresentation", "hydra:BasicRepresentation", true);
            
            await writer.WritePropertyNameAsync("hydra:mapping");
            await writer.WriteArrayOpenAsync();
            await writer.WriteOpenAsync();
            await writer.WritePropertyAsync("@type", "hydra:IriTemplateMapping", true);
            await writer.WritePropertyAsync("hydra:variable", "x", true);
            await writer.WritePropertyAsync("hydra:property", "tiles:longitudeTile", true);
            await writer.WritePropertyAsync("hydra:required", "true");
            await writer.WriteCloseAsync();
            await writer.WriteOpenAsync();
            await writer.WritePropertyAsync("@type", "hydra:IriTemplateMapping", true);
            await writer.WritePropertyAsync("hydra:variable", "y", true);
            await writer.WritePropertyAsync("hydra:property", "tiles:latitudeTile", true);
            await writer.WritePropertyAsync("hydra:required", "true");
            await writer.WriteCloseAsync();
            await writer.WriteArrayCloseAsync();
            await writer.WriteCloseAsync();
            
            await writer.WriteCloseAsync();
        }

        internal static async Task WriteNodeAsync(this JsonWriter writer, Node node, Dictionary<string, TagMapperConfig> mapping)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            await writer.WriteOpenAsync();

            await writer.WritePropertyAsync("@id", $"http://www.openstreetmap.org/node/{node.Id}", true, false);
            await writer.WritePropertyAsync("@type", "osm:Node", true, true);
            await writer.WritePropertyAsync("geo:long", node.Longitude.ToInvariantString());
            await writer.WritePropertyAsync("geo:lat", node.Latitude.ToInvariantString());
            
            if (node.Tags != null)
            {
                await writer.WriteTagsAsync(node.Tags, mapping);
            }

            await writer.WriteCloseAsync();
        }

        internal static async Task WriteWayAsync(this JsonWriter writer, Way way, Dictionary<string, TagMapperConfig> mapping)
        {
            if (writer == null) { throw new ArgumentNullException(nameof(writer)); }
            if (way == null) { throw new ArgumentNullException(nameof(way)); }
            
            await writer.WriteOpenAsync();
            
            await writer.WritePropertyAsync("@id", $"http://www.openstreetmap.org/way/{way.Id}", true, false);
            await writer.WritePropertyAsync("@type", "osm:Way", true, true);

            if (way.Tags != null)
            {
                await writer.WriteTagsAsync(way.Tags, mapping);
            }
            
            await writer.WritePropertyNameAsync("osm:hasNodes");
            
            await writer.WriteArrayOpenAsync();
            if (way.Nodes != null)
            {
                foreach (var node in way.Nodes)
                {
                    await writer.WriteArrayValueAsync($"http://www.openstreetmap.org/node/{node}", true, false);
                }
            }
            await writer.WriteArrayCloseAsync();
            
            await writer.WriteCloseAsync();
        }

        internal static async Task WriteRelationAsync(this JsonWriter writer, Relation relation, Dictionary<string, TagMapperConfig> mapping)
        {
            if (writer == null) { throw new ArgumentNullException(nameof(writer)); }
            if (relation == null) { throw new ArgumentNullException(nameof(relation)); }
            
            await writer.WriteOpenAsync();
            
            await writer.WritePropertyAsync("@id", $"http://www.openstreetmap.org/relation/{relation.Id}", true, false);
            await writer.WritePropertyAsync("@type", "osm:Relation", true, true);

            if (relation.Tags != null)
            {
                await writer.WriteTagsAsync(relation.Tags, mapping);
            }
            
            await writer.WritePropertyNameAsync("osm:hasMembers");
            
            await writer.WriteArrayOpenAsync();
            if (relation.Members != null)
            {
                foreach (var member in relation.Members)
                {
                    await writer.WriteOpenAsync();

                    switch (member.Type)
                    {
                        case OsmGeoType.Node:
                            await writer.WritePropertyAsync("@id", $"http://www.openstreetmap.org/node/{member.Id}", true, false);
                            break;
                        case OsmGeoType.Way:
                            await writer.WritePropertyAsync("@id", $"http://www.openstreetmap.org/way/{member.Id}", true, false);
                            break;
                        case OsmGeoType.Relation:
                            await writer.WritePropertyAsync("@id", $"http://www.openstreetmap.org/relation/{member.Id}", true, false);
                            break;
                    }

                    if (!string.IsNullOrWhiteSpace(member.Role))
                    {
                        await writer.WritePropertyAsync("role", member.Role, true, false);
                    }
                    
                    await writer.WriteCloseAsync();
                }
            }
            await writer.WriteArrayCloseAsync();
            
            await writer.WriteCloseAsync();
        }

        internal static async Task WriteTagsAsync(this JsonWriter writer, TagsCollectionBase tags, Dictionary<string, TagMapperConfig> mapping)
        {
            var undefinedTags = new List<Tag>();
            foreach (var tag in tags)
            {
                if (await tag.Map(mapping, writer)) continue;
                
                undefinedTags.Add(tag);
            }
            
            await writer.WritePropertyNameAsync("osm:hasTag");
            await writer.WriteArrayOpenAsync();
            foreach (var tag in undefinedTags)
            {
                await writer.WriteArrayValueAsync($"{tag.Key}={tag.Value}", true, true);
            }
            await writer.WriteArrayCloseAsync();
        }
    }
}
