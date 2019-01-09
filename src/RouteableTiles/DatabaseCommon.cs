using System.IO;
using System.IO.Compression;
using OsmSharp;
using OsmSharp.Streams;
using OsmSharp.Streams.Filters;
using RoutableTiles.Build.Indexes;
using RoutableTiles.IO;
using RoutableTiles.Tiles;

namespace RoutableTiles
{
    internal static class DatabaseCommon
    {
        private static Stream CreateInflateStream(Stream stream)
        {
            //return new DeflateStream(stream, CompressionMode.Decompress);
            return new GZipStream(stream, CompressionMode.Decompress);
        }

        private static Stream CreateDeflateStream(Stream stream)
        {
            //return new DeflateStream(stream, CompressionLevel.Fastest);
            return new GZipStream(stream, CompressionLevel.Fastest);
        }
        
        /// <summary>
        /// Loads one tile.
        /// </summary>
        public static Stream LoadTile(string path, OsmGeoType type, Tile tile, bool compressed = false)
        {
            var location = DatabaseCommon.BuildPathToTile(path, type, tile, compressed);

            if (!FileSystemFacade.FileSystem.Exists(location))
            {
                return null;
            }

            if (compressed)
            {
                return CreateInflateStream(FileSystemFacade.FileSystem.OpenRead(location));
            }

            return FileSystemFacade.FileSystem.OpenRead(location);
        }
        
        /// <summary>
        /// Creates a tile.
        /// </summary>
        public static Stream CreateTile(string path, OsmGeoType type, Tile tile, bool compressed = false)
        {
            var location = DatabaseCommon.BuildPathToTile(path, type, tile, compressed);

            var fileDirectory = FileSystemFacade.FileSystem.DirectoryForFile(location);
            if (!FileSystemFacade.FileSystem.DirectoryExists(fileDirectory))
            {
                FileSystemFacade.FileSystem.CreateDirectory(fileDirectory);
            }

            if (compressed)
            {
                return CreateDeflateStream(FileSystemFacade.FileSystem.Open(location, FileMode.Create));
            }

            return FileSystemFacade.FileSystem.Open(location, FileMode.Create);
        }

        /// <summary>
        /// Builds a path to the given tile.
        /// </summary>
        public static string BuildPathToTile(string path, OsmGeoType type, Tile tile, bool compressed = false)
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

            if (compressed)
            {
                return location + ".zip";
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