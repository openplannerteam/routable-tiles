using System;
using System.Collections.Generic;
using OsmSharp;
using OsmSharp.Streams;
using OsmSharp.Streams.Filters;
using RoutableTiles.Tiles;

namespace RoutableTiles.IO.JsonLD
{
    /// <summary>
    /// An OSM stream target to write one routable tile.
    /// </summary>
    public class TileOsmStreamTarget : OsmStreamFilter
    {
        private readonly (uint x, uint y, uint zoom) _tile;
        private readonly Dictionary<long, Node> _nodesInTile;

        public TileOsmStreamTarget(uint x, uint y, uint zoom)
        {
            _tile = (x, y, zoom);
        }

        public override bool MoveNext(bool ignoreNodes, bool ignoreWays, bool ignoreRelations)
        {
            if (!this.Source.MoveNext(ignoreNodes, ignoreWays, ignoreRelations)) return false;

            var osmGeo = this.Source.Current();

            switch (osmGeo.Type)
            {
                case OsmGeoType.Node:
                    
                    break;
                case OsmGeoType.Way:
                    break;
                case OsmGeoType.Relation:
                    break;
            }

            return true;
        }
        
        /// <summary>
        /// Converts lat/lon to tile coordinates.
        /// </summary>
        private static Tile WorldToTileIndex(double latitude, double longitude, uint zoom)
        {
            var n = (int)Math.Floor(Math.Pow(2, zoom));

            var rad = (latitude / 180d) * System.Math.PI;

            var x = (uint)((longitude + 180.0f) / 360.0f * n);
            var y = (uint)(
                (1.0f - Math.Log(Math.Tan(rad) + 1.0f / Math.Cos(rad))
                 / Math.PI) / 2f * n);

            return new Tile { X = x, Y = y, Zoom = zoom };
        }

        public override OsmGeo Current()
        {
            throw new System.NotImplementedException();
        }

        public override void Reset()
        {
            throw new System.NotImplementedException();
        }

        public override void RegisterSource(OsmStreamSource source)
        {
            if (!source.IsSorted)
            {
                throw new Exception($"{nameof(TileOsmStreamTarget)} can only handle sorted source streams.");
            }
            base.RegisterSource(source);
        }

        public override bool CanReset { get; }
    }
}