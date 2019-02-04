using System;
using System.Collections.Generic;
using System.IO.Compression;
using OsmSharp;
using OsmSharp.Streams;
using RouteableTiles.Build.Indexes;
using Serilog;
using RouteableTiles.IO;
using RouteableTiles.Tiles;

namespace RouteableTiles.Build
{

    /// <summary>
    /// Builds a database from scratch and writes the structure to the given folder.
    /// </summary>
    public static class Builder
    {
        /// <summary>
        /// Builds a new database and write the structure to the given path.
        /// </summary>
        public static void Build(OsmStreamSource source, string path, uint maxZoom = 12, bool compressed = false)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            if (path == null) { throw new ArgumentNullException(nameof(path)); }
            if (!source.CanReset) { throw new ArgumentException("Source cannot be reset."); }
            if (!FileSystemFacade.FileSystem.DirectoryExists(path)) { throw new ArgumentException("Output path does not exist."); }
            
            Log.Logger.Information("Building for tile {0}/{1}/{2}...", 0, 0, 0);
            var tiles = BuildInitial(source, path, maxZoom, new Tile(0, 0, 0), compressed);
            while (true)
            {
                var newTiles = new List<Tile>();

                System.Threading.Tasks.Parallel.For(0, tiles.Count, (t) =>
                {
                    var subTile = tiles[t];
                    Log.Logger.Information($"Building for tile ({t + 1}/{tiles.Count}):{subTile.Zoom}/{subTile.X}/{subTile.Y}...");
                    var subTiles = Build(path, maxZoom, subTile, compressed);

                    lock (newTiles)
                    {
                        newTiles.AddRange(subTiles);
                    }
                });

                if (newTiles.Count == 0)
                {
                    break;
                }

                tiles = newTiles;
            }
        }

        private static List<Tile> BuildInitial(OsmStreamSource source, string path, uint maxZoom, Tile tile, bool compressed = false)
        {
            // split nodes and return nodes index and non-empty tiles.
            var nodeIndex = NodeProcessor.Process(source, path, maxZoom, tile, out var nonEmptyTiles, out var hasNext, compressed);

            // split ways using the node index and return the way index.
            Index wayIndex = null;
            if (hasNext)
            {
                wayIndex = WayProcessor.Process(source, path, maxZoom, tile, nodeIndex, compressed);
            }

            // split relations using the node and way index and return the relation index.
            var relationIndex = RelationProcessor.Process(source, path, maxZoom, tile, nodeIndex, wayIndex, compressed);

            // write the indices to disk.
            var actions = new List<Action>
            {
                () => nodeIndex.Write(FileSystemFacade.FileSystem.Combine(path, tile.Zoom.ToString(),
                    tile.X.ToString(), tile.Y.ToString() + ".nodes.idx")),
                () => wayIndex?.Write(FileSystemFacade.FileSystem.Combine(path, tile.Zoom.ToString(),
                    tile.X.ToString(), tile.Y.ToString() + ".ways.idx")),
                () => relationIndex?.Write(FileSystemFacade.FileSystem.Combine(path, tile.Zoom.ToString(),
                    tile.X.ToString(), tile.Y.ToString() + ".relations.idx"))
            };
            System.Threading.Tasks.Parallel.ForEach(actions, (a) => a());
            
            return nonEmptyTiles;
        }
        
        /// <summary>
        /// Builds the database and writes the structure to the given by by splitting the given zoom level.
        /// </summary>
        private static List<Tile> Build(string path, uint maxZoom, Tile tile, bool compressed = false)
        {
            // split nodes and return index and non-empty tiles.
            List<Tile> nonEmptyTiles = null;
            Index nodeIndex = null;
            
            var nodeFile = DatabaseCommon.BuildPathToTile(path, OsmGeoType.Node, tile, compressed);
            if (!FileSystemFacade.FileSystem.Exists(nodeFile))
            {
                Log.Logger.Warning("Tile {0}/{1}/{2} not found: {3}", tile.Zoom, tile.X, tile.Y,
                    nodeFile);
                return new List<Tile>();
            }
            using (var nodeStream = DatabaseCommon.LoadTile(path, OsmGeoType.Node, tile, compressed))
            {
                var nodeSource = new OsmSharp.Streams.BinaryOsmStreamSource(nodeStream);

                // split nodes and return nodes index and non-empty tiles.
                nodeIndex = NodeProcessor.Process(nodeSource, path, maxZoom, tile, out nonEmptyTiles,
                    out _, compressed);
            }

            // build the ways index.
            Index wayIndex = null;
            var wayFile = DatabaseCommon.BuildPathToTile(path, OsmGeoType.Way, tile, compressed);
            if (FileSystemFacade.FileSystem.Exists(wayFile))
            {
                using (var wayStream = DatabaseCommon.LoadTile(path, OsmGeoType.Way, tile, compressed))
                {
                    var waySource = new OsmSharp.Streams.BinaryOsmStreamSource(wayStream);
                    if (waySource.MoveNext())
                    {
                        wayIndex = WayProcessor.Process(waySource, path, maxZoom, tile, nodeIndex, compressed);
                    }
                }
            }  

            // build the relations index.
            Index relationIndex = null;
            var relationFile = DatabaseCommon.BuildPathToTile(path, OsmGeoType.Relation, tile, compressed);
            if (FileSystemFacade.FileSystem.Exists(relationFile))
            {
                using (var relationStream = DatabaseCommon.LoadTile(path, OsmGeoType.Relation, tile, compressed))
                {
                    var relationSource = new OsmSharp.Streams.BinaryOsmStreamSource(relationStream);
                    if (relationSource.MoveNext())
                    {
                        relationIndex = RelationProcessor.Process(relationSource, path, maxZoom, tile, nodeIndex, wayIndex, compressed);
                    }
                }
            }

            // write the indexes to disk.
            var actions = new List<Action>
            {
                () => nodeIndex.Write(FileSystemFacade.FileSystem.Combine(path, tile.Zoom.ToString(),
                    tile.X.ToString(), tile.Y.ToString() + ".nodes.idx")),
                () => wayIndex?.Write(FileSystemFacade.FileSystem.Combine(path, tile.Zoom.ToString(),
                    tile.X.ToString(), tile.Y.ToString() + ".ways.idx")),
                () => relationIndex?.Write(FileSystemFacade.FileSystem.Combine(path, tile.Zoom.ToString(),
                    tile.X.ToString(), tile.Y.ToString() + ".relations.idx"))
            };
            System.Threading.Tasks.Parallel.ForEach(actions, (a) => a());

            if (FileSystemFacade.FileSystem.Exists(nodeFile))
            {
                FileSystemFacade.FileSystem.Delete(nodeFile);
            }
            if (FileSystemFacade.FileSystem.Exists(wayFile))
            {
                FileSystemFacade.FileSystem.Delete(wayFile);
            }
            if (FileSystemFacade.FileSystem.Exists(relationFile))
            {
                FileSystemFacade.FileSystem.Delete(relationFile);
            }

            return nonEmptyTiles;
        }
    }
}