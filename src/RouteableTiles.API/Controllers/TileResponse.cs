using System.Collections.Generic;
using OsmSharp;
using RouteableTiles.Tiles;

namespace RouteableTiles.API.Controllers
{
    internal class TileResponse
    {
        public IEnumerable<OsmGeo> Data { get; set; }

        public Tile Tile { get; set; }
    }
}