using System;
using System.Collections.Generic;
using System.IO;
using OsmSharp;
using OsmSharp.Streams;
using OsmSharp.Streams.Filters;
using RouteableTiles.Tiles;

namespace RouteableTiles.IO.JsonLD
{
    /// <summary>
    /// An OSM stream target to write one routable tile.
    /// </summary>
    public class TileOsmStreamTarget : OsmStreamTarget, IDisposable
    {
        private readonly Stream _stream;

        public TileOsmStreamTarget(Stream stream)
        {
            _stream = stream;
        }

        private JsonWriter _jsonWriter;

        public override void Initialize()
        {
            _jsonWriter = new JsonWriter(new StreamWriter(_stream));
            _jsonWriter.WriteOpen();
            
            _jsonWriter.WriteContext();
            
            _jsonWriter.WritePropertyName("@graph");
            _jsonWriter.WriteArrayOpen();
        }

        public override void AddNode(Node node)
        {
            _jsonWriter.WriteNode(node);
        }

        public override void AddWay(Way way)
        {
            _jsonWriter.WriteWay(way);
        }

        public override void AddRelation(Relation relation)
        {
            _jsonWriter.WriteRelation(relation);
        }

        public override void Flush()
        {
            _jsonWriter.WriteArrayClose();
            _jsonWriter.WriteClose();
            _jsonWriter.Flush();

            base.Flush();
        }

        /// <summary>
        /// Releases all resources used by this object.
        /// </summary>
        public void Dispose()
        {
            _jsonWriter.Dispose();
        }
    }
}