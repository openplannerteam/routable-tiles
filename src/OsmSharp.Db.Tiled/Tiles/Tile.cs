using System;
using System.Collections.Generic;

namespace OsmSharp.Db.Tiled.Tiles
{
    /// <summary>
    /// Represents a tile.
    /// </summary>
    public class Tile
    {
        /// <summary>
        /// Creates a new tile.
        /// </summary>
        public Tile()
        {

        }

        /// <summary>
        /// Creates a new tile.
        /// </summary>
        public Tile(uint x, uint y, uint zoom)
        {
            this.X = x;
            this.Y = y;
            this.Zoom = zoom;
        }

        /// <summary>
        /// Gets or sets X.
        /// </summary>
        /// <returns></returns>
        public uint X { get; set; }

        /// <summary>
        /// Gets or sets Y.
        /// </summary>
        /// <returns></returns>
        public uint Y { get; set; }

        /// <summary>
        /// Gets or sets zoom.
        /// </summary>
        /// <returns></returns>
        public uint Zoom { get; set; }

        /// <summary>
        /// Returns an id unique per zoom level.
        /// </summary>
        /// <returns></returns>
        public ulong LocalId
        {
            get
            {
                ulong xMax = (ulong)(1 << (int)this.Zoom);

                return this.X + this.Y * xMax;
            }
        }       
        
        /// <summary>
        /// Builds a tile from a local id and zoom.
        /// </summary>
        public static Tile FromLocalId(uint zoom, ulong localId)
        {
            ulong xMax = (ulong)(1 << (int)zoom);

            return new Tile()
            {
                X = (uint)(localId % xMax),
                Y = (uint)(localId / xMax),
                Zoom = zoom
            };
        }

        /// <summary>
        /// Gets the subtiles for this tile at zoom+1.
        /// </summary>
        /// <returns></returns>
        public Tile[] Subtiles
        {
            get
            {
                var x = this.X * 2;
                var y = this.Y * 2;
                return new Tile[]
                {
                    new Tile()
                    {
                        Zoom = this.Zoom + 1,
                        X = x,
                        Y = y
                    },
                    new Tile()
                    {
                        Zoom = this.Zoom + 1,
                        X = x + 1,
                        Y = y
                    },
                    new Tile()
                    {
                        Zoom = this.Zoom + 1,
                        X = x,
                        Y = y + 1
                    },
                    new Tile()
                    {
                        Zoom = this.Zoom + 1,
                        X = x + 1,
                        Y = y + 1
                    }
                };
            }
        }
        
        /// <summary>
        /// Gets the subtiles for this tile at the given zoom level.
        /// </summary>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public List<Tile> GetSubtilesAt(uint zoom)
        {
            if (zoom <= this.Zoom) { throw new ArgumentOutOfRangeException(nameof(zoom)); }

            var tiles = new List<Tile>();
            if (this.Zoom + 1 == zoom)
            {
                tiles.AddRange(this.Subtiles);
            }
            else
            {
                foreach(var tile in this.Subtiles)
                {
                    tiles.AddRange(tile.GetSubtilesAt(zoom));
                }
            }
            return tiles;
        }

        /// <summary>
        /// Converts lat/lon to tile coordinates.
        /// </summary>
        public static Tile WorldToTileIndex(double latitude, double longitude, uint zoom)
        {
            var n = (int)Math.Floor(Math.Pow(2, zoom));

            var rad = (latitude / 180d) * System.Math.PI;

            var x = (uint)((longitude + 180.0f) / 360.0f * n);
            var y = (uint)(
                (1.0f - Math.Log(Math.Tan(rad) + 1.0f / Math.Cos(rad))
                / Math.PI) / 2f * n);

            return new Tile { X = x, Y = y, Zoom = zoom };
        }

        public override string ToString()
        {
            return string.Format("{0}/{1}/{2}", this.Zoom, this.X, this.Y);
        }

        public override int GetHashCode()
        {
            return this.X.GetHashCode() ^
                this.Y.GetHashCode() ^
                this.Zoom.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var other = (obj as Tile);
            if (other == null)
            {
                return false;
            }
            return other.X == this.X &&
                other.Y == this.Y &&
                other.Zoom == this.Zoom;
        }
    }
}