using OsmSharp.Db.Tiled.Indexes;
using OsmSharp.Db.Tiled.Tiles;
using OsmSharp.Db.Tiled.IO;
using System.IO;

namespace OsmSharp.Db.Tiled
{
    /// <summary>
    /// Contains a few common static database functions.
    /// </summary>
    public static class DatabaseCommon
    {
        /// <summary>
        /// Finds a tile by location.
        /// </summary>
        public static Tile FindTileByLocation(uint zoom, double latitude, double longitude)
        {
            return Tiles.Tile.WorldToTileIndex(latitude, longitude, zoom);
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

        /// <summary>
        /// Saves the given index to disk.
        /// </summary>
        public static void SaveIndex(string path, Tile tile, OsmGeoType type, Index index)
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
            var locationPath = FileSystemFacade.FileSystem.DirectoryForFile(location);
            if (!FileSystemFacade.FileSystem.DirectoryExists(locationPath))
            {
                FileSystemFacade.FileSystem.CreateDirectory(locationPath);
            }
            using (var stream = FileSystemFacade.FileSystem.Open(location, FileMode.Create))
            {
                index.Serialize(stream);
            }
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
        /// Appends a new object to the given tile.
        /// </summary>
        public static void AppendToTile(string path, Tile tile, OsmGeo osmGeo)
        {
            var location = DatabaseCommon.BuildPathToTile(path, osmGeo.Type, tile);

            var locationPath = FileSystemFacade.FileSystem.DirectoryForFile(location);
            if (!FileSystemFacade.FileSystem.DirectoryExists(locationPath))
            {
                FileSystemFacade.FileSystem.CreateDirectory(locationPath);
            }

            Stream stream;
            if (!FileSystemFacade.FileSystem.Exists(location))
            {
                stream = FileSystemFacade.FileSystem.Open(location, FileMode.Create);
            }
            else
            {
                stream = FileSystemFacade.FileSystem.OpenWrite(location);
            }

            using (stream)
            {
                switch(osmGeo.Type)
                {
                    case OsmGeoType.Node:
                        OsmSharp.IO.Binary.BinarySerializer.Append(stream, osmGeo as Node);
                        break;
                    case OsmGeoType.Way:
                        OsmSharp.IO.Binary.BinarySerializer.Append(stream, osmGeo as Way);
                        break;
                    case OsmGeoType.Relation:
                        OsmSharp.IO.Binary.BinarySerializer.Append(stream, osmGeo as Relation);
                        break;
                }
            }
        }
    }
}