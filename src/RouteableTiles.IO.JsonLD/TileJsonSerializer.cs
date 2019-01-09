using System;
using System.Runtime.CompilerServices;
using OsmSharp;

[assembly: InternalsVisibleTo("RouteableTiles.Tests")]
[assembly: InternalsVisibleTo("RouteableTiles.Tests.Functional")]
namespace RouteableTiles.IO.JsonLD
{
    internal static class JsonSerializer
    {
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
            writer.WriteProperty("hydra:template", "https://tiles.openplanner.team/planet/{x}/{y}/14", true);
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
            if (writer == null) { throw new ArgumentNullException(nameof(writer)); }
            if (node == null) { throw new ArgumentNullException(nameof(node)); }
            
            writer.WriteOpen();
            
            writer.WriteProperty("@id", $"http://www.openstreetmap.org/node/{node.Id}", true, false);
            writer.WriteProperty("geo:long", node.Longitude.ToInvariantString());
            writer.WriteProperty("geo:lat", node.Latitude.ToInvariantString());
            
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
    }
}