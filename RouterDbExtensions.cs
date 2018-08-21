using System;
using System.Collections.Generic;
using System.IO;
using Itinero;
using Itinero.Algorithms.Search.Hilbert;
using Itinero.Attributes;
using Itinero.Data.Network;
using Itinero.IO.Json;
using Itinero.IO.Osm;
using Itinero.LocalGeo;
using routable_tiles.Tiles;
using Serilog;

namespace routable_tiles
{
    /// <summary>
    /// Contains routerdb extensions.
    /// </summary>
    public static class RouterDbExtensions
    {
        /// <summary>
        /// Gets a routable geojson tile.
        /// </summary>
        public static bool WriteRoutingTile(this RouterDb db, TextWriter writer, Tile tile, Func<uint, long> getGlobalId)
        {
            if (db == null) { throw new ArgumentNullException("db"); }
            if (writer == null) { throw new ArgumentNullException("writer"); }

            var box = tile.Box;
            var minLatitude = box.MinLat;
            var maxLatitude = box.MaxLat;
            var minLongitude = box.MinLon;
            var maxLongitude = box.MaxLon;

            var vertices = HilbertExtensions.Search(db.Network.GeometricGraph, minLatitude, minLongitude,
                maxLatitude, maxLongitude);
            var vertexIds = new Dictionary<uint, long>();
            var edges = new HashSet<long>();

            if (vertices.Count == 0)
            {
                return false;
            }

            var jsonWriter = new JsonWriter(writer);
            jsonWriter.WriteOpen();
            jsonWriter.WriteProperty("type", "FeatureCollection", true, false);
            jsonWriter.WritePropertyName("features", false);
            jsonWriter.WriteArrayOpen();

            tile.WriteTile(jsonWriter);

            var edgeEnumerator = db.Network.GetEdgeEnumerator();
            foreach (var vertex in vertices)
            {
                var location = db.Network.GetVertex(vertex);
                if (!(minLatitude <= location.Latitude && location.Latitude < maxLatitude &&
                      minLongitude <= location.Longitude && location.Longitude < maxLongitude))
                {
                    continue;
                }

                edgeEnumerator.MoveTo(vertex);
                if (!vertexIds.ContainsKey(vertex))
                {
                    vertexIds[vertex] = vertex;
                }
                edgeEnumerator.Reset();
                while (edgeEnumerator.MoveNext())
                {
                    if (edges.Contains(edgeEnumerator.Id))
                    {
                        continue;
                    }
                    edges.Add(edgeEnumerator.Id);

                    if (!vertices.Contains(edgeEnumerator.To))
                    {
                        vertexIds[edgeEnumerator.From] = getGlobalId(edgeEnumerator.From);
                        vertexIds[edgeEnumerator.To] = getGlobalId(edgeEnumerator.To);
                    }
                }
            }

            edges.Clear();
            foreach (var vertexId in vertexIds)
            {
                edgeEnumerator.MoveTo(vertexId.Key);
                edgeEnumerator.Reset();
                while (edgeEnumerator.MoveNext())
                {
                    if (edges.Contains(edgeEnumerator.Id))
                    {
                        continue;
                    }
                    edges.Add(edgeEnumerator.Id);

                    long fromVertexId, toVertexId;
                    if (!vertexIds.TryGetValue(edgeEnumerator.From, out fromVertexId) ||
                        !vertexIds.TryGetValue(edgeEnumerator.To, out toVertexId))
                    {
                        continue;
                    }

                    var edgeAttributes = new Itinero.Attributes.AttributeCollection(db.EdgeMeta.Get(edgeEnumerator.Data.MetaId));
                    edgeAttributes.AddOrReplace(db.EdgeProfiles.Get(edgeEnumerator.Data.Profile));

                    var shape = db.Network.GetShape(edgeEnumerator.Current);
                    
                    jsonWriter.WriteOpen();
                    jsonWriter.WriteProperty("type", "Feature", true, false);
                    jsonWriter.WritePropertyName("geometry", false);

                    jsonWriter.WriteOpen();
                    jsonWriter.WriteProperty("type", "LineString", true, false);
                    jsonWriter.WritePropertyName("coordinates", false);
                    jsonWriter.WriteArrayOpen();

                    foreach (var coordinate in shape)
                    {
                        jsonWriter.WriteArrayOpen();
                        jsonWriter.WriteArrayValue(coordinate.Longitude.ToInvariantString());
                        jsonWriter.WriteArrayValue(coordinate.Latitude.ToInvariantString());
                        jsonWriter.WriteArrayClose();
                    }

                    jsonWriter.WriteArrayClose();
                    jsonWriter.WriteClose();

                    jsonWriter.WritePropertyName("properties");
                    jsonWriter.WriteOpen();
                    if (edgeAttributes != null)
                    {
                        foreach (var attribute in edgeAttributes)
                        {
                            jsonWriter.WriteProperty(attribute.Key, attribute.Value, true, true);
                        }
                    }
                    jsonWriter.WriteProperty("vertex1", fromVertexId.ToInvariantString());
                    jsonWriter.WriteProperty("vertex2", toVertexId.ToInvariantString());
                    jsonWriter.WriteClose();

                    jsonWriter.WriteClose();
                }
            }

            jsonWriter.WriteArrayClose();
            jsonWriter.WriteClose();

            return true;
        }

        internal static void WriteTile(this Tiles.Tile tile, JsonWriter jsonWriter)
        {
            var box = tile.Box;

            var corners = new Coordinate[]
            {
                new Coordinate(box.MaxLat, box.MinLon),
                new Coordinate(box.MaxLat, box.MaxLon),
                new Coordinate(box.MinLat, box.MaxLon),
                new Coordinate(box.MinLat, box.MinLon),
                new Coordinate(box.MaxLat, box.MinLon)
            };

            jsonWriter.WriteOpen();
            jsonWriter.WriteProperty("type", "Feature", true, false);
            jsonWriter.WritePropertyName("geometry", false);

            jsonWriter.WriteOpen();
            jsonWriter.WriteProperty("type", "LineString", true, false);
            jsonWriter.WritePropertyName("coordinates", false);
            jsonWriter.WriteArrayOpen();

            foreach (var coordinate in corners)
            {
                jsonWriter.WriteArrayOpen();
                jsonWriter.WriteArrayValue(coordinate.Longitude.ToInvariantString());
                jsonWriter.WriteArrayValue(coordinate.Latitude.ToInvariantString());
                jsonWriter.WriteArrayClose();
            }

            jsonWriter.WriteArrayClose();
            jsonWriter.WriteClose();

            jsonWriter.WritePropertyName("properties");
            jsonWriter.WriteOpen();
            jsonWriter.WriteProperty("id", tile.LocalId.ToInvariantString(), true, true);
            jsonWriter.WriteProperty("zoom", tile.Zoom.ToInvariantString(), true, true);
            jsonWriter.WriteClose();

            jsonWriter.WriteClose();
        }
    }
}