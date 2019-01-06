using System.IO;
using OsmSharp;
using RoutableTiles.Build.Indexes;
using RoutableTiles.IO;
using RoutableTiles.Tiles;

namespace RoutableTiles
{
    internal static class DatabaseCommon
    {
        /// <summary>
        /// Loads one tile.
        /// </summary>
        public static Stream LoadTile(string path, OsmGeoType type, Tile tile)
        {
            var location = DatabaseCommon.BuildPathToTile(path, type, tile);

            if (!FileSystemFacade.FileSystem.Exists(location))
            {
                return null;
            }
            return FileSystemFacade.FileSystem.OpenRead(location);
        }

        /// <summary>
        /// Builds a path to the given tile.
        /// </summary>
        public static string BuildPathToTile(string path, OsmGeoType type, Tile tile)
        {
            var location = FileSystemFacade.FileSystem.Combine(path, tile.Zoom.ToInvariantString(),
                tile.X.ToInvariantString());
            if (type == OsmGeoType.Node)
            {
                location = FileSystemFacade.FileSystem.Combine(location, tile.Y.ToInvariantString() + ".nodes.osm.bin");
            }
            else if (type == OsmGeoType.Way)
            {
                location = FileSystemFacade.FileSystem.Combine(location, tile.Y.ToInvariantString() + ".ways.osm.bin");
            }
            else
            {
                location = FileSystemFacade.FileSystem.Combine(location, tile.Y.ToInvariantString() + ".relations.osm.bin");
            }
            return location;
        }
        
        /// <summary>
        /// Loads an index for the given tile from disk (if any).
        /// </summary>
        public static Index LoadIndex(string path, Tile tile, OsmGeoType type)
        {
            var extension = ".nodes.idx";
            if (type == OsmGeoType.Way)
            {
                extension = ".ways.idx";
            }
            else if (type == OsmGeoType.Relation)
            {
                extension = ".relations.idx";
            }

            var location = FileSystemFacade.FileSystem.Combine(path, tile.Zoom.ToInvariantString(),
                tile.X.ToInvariantString(), tile.Y.ToInvariantString() + extension);
            if (!FileSystemFacade.FileSystem.Exists(location))
            {
                return null;
            }
            using (var stream = FileSystemFacade.FileSystem.OpenRead(location))
            {
                return Index.Deserialize(stream);
            }
        }
    }
}