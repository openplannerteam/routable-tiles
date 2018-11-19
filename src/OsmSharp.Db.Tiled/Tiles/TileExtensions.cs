using System;
using System.Collections.Generic;

namespace OsmSharp.Db.Tiled.Tiles
{
    /// <summary>
    /// Contains extension methods for the tile class.
    /// </summary>
    public static class TileExtensions
    {
        /// <summary>
        /// Builds a mask for the given tile relative to a tile at a zoom level - 2.
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
        /// Gets a parent tile at the 'higher' zoom.
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public static Tile ParentTileAt(this Tile tile, uint zoom)
        {
            if (tile.Zoom < zoom) { throw new ArgumentException("Zoom level needs to be smaller or equal."); }

            var z = tile.Zoom;
            var x = tile.X;
            var y = tile.Y;
            while (z > zoom)
            {
                x = x / 2;
                y = y / 2;
                z--;
            }
            return new Tile(x, y, z);
        }
    }
}