using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using OsmSharp;
using OsmSharp.Tags;

[assembly: InternalsVisibleTo("RouteableTiles.Tests")]
[assembly: InternalsVisibleTo("RouteableTiles.Tests.Functional")]
namespace RouteableTiles.IO.JsonLD
{
    public static class JsonSerializer
    {
        /// <summary>
        /// Contains all supported vehicle types, see:
        ///
        /// https://wiki.openstreetmap.org/wiki/Key:access
        /// </summary>
        private static HashSet<string> VehicleTypes = new HashSet<string>(new[]
        {
            "access",
            "bicycle",
            "foot",
            "ski",
            "horse",
            "vehicle",
            "carriage",
            "trailer",
            "caravan",
            "motor_vehicle",
            "motorcycle",
            "moped",
            "mofa",
            "motorocar",
            "motorhome",
            "tourist_bus",
            "coach",
            "goods",
            "hgv",
            "hgv_articulated",
            "agricultural",
            "atv",
            "snowmobile",
            "psv",
            "bus",
            "minibus",
            "share_taxi",
            "taxi",
            "hov",
            "car_sharing",
            "emergency",
            "hazmat",
            "disabled"
        });
        
        /// <summary>
        /// Writes the given enumerable of osm geo objects to the given text writer in JSON-LD routeable tiles format.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="writer">The writer.</param>
        public static void WriteTo(this IEnumerable<OsmGeo> data, TextWriter writer)
        {
            var jsonWriter = new JsonWriter(writer);
            jsonWriter.WriteOpen();
            
            jsonWriter.WriteContext();
            
            jsonWriter.WritePropertyName("@graph");
            jsonWriter.WriteArrayOpen();

            foreach (var osmGeo in data)
            {
                switch (osmGeo)
                {
                    case Node node:
                        jsonWriter.WriteNode(node);
                        break;
                    case Way way:
                        jsonWriter.WriteWay(way);
                        break;
                    case Relation _:
                        //_jsonWriter.WriteRelation()
                        break;
                }
            }
            
            jsonWriter.WriteArrayClose();
            jsonWriter.WriteClose();
            jsonWriter.Flush();
        }
        
        internal static void WriteContext(this JsonWriter writer)
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
            writer.WritePropertyName("osm:nodes");
            writer.WriteOpen();
            writer.WriteProperty("@container", "@list", true);
            writer.WriteProperty("@type", "@id", true);
            writer.WriteClose();
            writer.WriteClose();
            
            writer.WritePropertyName("dcterms:isPartOf");
            writer.WriteOpen();
            
            writer.WriteProperty("@id", "https://tiles.openplanner.team/planet", true);
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

        internal static void WriteNode(this JsonWriter writer, Node node)
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
                if (node.Tags.TryGetValue("barrier", out var barrier))
                {
                    writer.WriteProperty("osm:barrier", "osm:" + barrier, true, true);
                    foreach (var tag in node.Tags)
                    {
                        writer.WriteAccessTag(tag);
                    }
                }
            }

            writer.WriteClose();
        }

        internal static void WriteWay(this JsonWriter writer, Way way)
        {
            if (writer == null) { throw new ArgumentNullException(nameof(writer)); }
            if (way == null) { throw new ArgumentNullException(nameof(way)); }
            
            writer.WriteOpen();
            
            writer.WriteProperty("@id", $"http://www.openstreetmap.org/way/{way.Id}", true, false);
            writer.WriteProperty("@type", "osm:Way", true, true);

            if (way.Tags != null)
            {
                foreach (var tag in way.Tags)
                {
                    if (writer.WriteAccessTag(tag)) continue;
                    
                    switch (tag.Key)
                    {
                        case "name":
                            writer.WriteProperty("rdfs:label", tag.Value, true, true);
                            break;
                        case "highway":
                            writer.WriteProperty("osm:highway", "osm:" + tag.Value, true, true);
                            break;
                        case "maxspeed":
                            writer.WriteProperty("osm:maxspeed", tag.Value, true, true);
                            break;
                    }
                }
            }
            
            writer.WritePropertyName("osm:nodes");
            
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

        internal static bool WriteAccessTag(this JsonWriter writer, Tag tag)
        {
            if (VehicleTypes.Contains(tag.Key))
            {
                writer.WriteProperty("osm:" + tag.Key, "osm:" + tag.Value, true, true);
                return true;
            }

            return false;
        }
    }
}
