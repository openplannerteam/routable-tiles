using System;
using System.Runtime.CompilerServices;
using OsmSharp;

[assembly: InternalsVisibleTo("RoutableTiles.Tests")]
[assembly: InternalsVisibleTo("RoutableTiles.Tests.Functional")]
namespace RoutableTiles.IO.JsonLD
{
    internal static class JsonSerializer
    {
        internal static void WriteNode(this JsonWriter writer, Node node)
        {
            if (writer == null) { throw new ArgumentNullException(nameof(writer)); }
            if (node == null) { throw new ArgumentNullException(nameof(node)); }
            
            writer.WriteOpen();
            
            writer.WriteProperty("@id", $"http://www.openstreetmap.org/node/{node.Id}", true, true);
            writer.WriteProperty("geo:long", node.Longitude.ToInvariantString());
            writer.WriteProperty("geo:lat", node.Longitude.ToInvariantString());
            
            writer.WriteClose();
        }
        
        internal static void WriteWay(this JsonWriter writer, Way way)
        {
            if (writer == null) { throw new ArgumentNullException(nameof(writer)); }
            if (way == null) { throw new ArgumentNullException(nameof(way)); }
            
            writer.WriteOpen();
            
            writer.WriteProperty("@id", $"http://www.openstreetmap.org/way/{way.Id}", true, true);
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
                            writer.WriteProperty("osm:highway", tag.Value, true, true);
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
                    writer.WriteArrayValue($"http://www.openstreetmap.org/node/{node}");
                }
            }
            writer.WriteArrayClose();
            
            writer.WriteClose();
        }
    }
}