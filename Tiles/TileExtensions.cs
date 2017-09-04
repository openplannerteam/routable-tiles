using System.Collections.Generic;
using Itinero.LocalGeo;

namespace routable_tiles.Tiles
{
    /// <summary>
    /// Contains extension methods for the tile class.
    /// </summary>
    public static class TileExtensions
    {
        /// <summary>
        /// Builds a mask for the given tile relative to a tile at a zoom level + 2.
        /// </summary>
        public static int BuildMask2(this Tile tile)
        {
            var xMod = tile.X % 4;
            var yMod = tile.Y % 4;

            return 1 << (int)(yMod * 4 + xMod);
        }

        /// <summary>
        /// Gets the subtiles for the given mask.
        /// </summary>
        public static IEnumerable<Tile> SubTilesForMask2(this Tile tile, int mask)
        {
            var baseX = tile.X * 4;
            var baseY = tile.Y * 4;
            for (var x = 0; x < 4; x++)
            {
                for (var y = 0; y < 4; y++)
                {
                    var i = y * 4 + x;
                    if ((mask & (1 << i)) != 0)
                    {
                        yield return new Tile(baseX + (uint)x, baseY + (uint)y, tile.Zoom + 2);
                    }
                }
            }
        }

        /// <summary>
        /// Enumerates the tiles covering the box.
        /// </summary>
        public static IEnumerable<Tile> GetTilesCovering(this Box box, uint zoom)
        {
            var topLeft = Tile.WorldToTileIndex(box.MaxLat, box.MinLon, zoom);
            var bottomRight = Tile.WorldToTileIndex(box.MinLat, box.MaxLon, zoom);

            for (var x = topLeft.X; x <= bottomRight.X; x++)
            {
                for (var y = topLeft.Y; y <= bottomRight.Y; y++)
                {
                    yield return new Tile(x, y, zoom);
                }
            }
        }
    }
}