using OsmSharp.Db.Tiled.Ids;
using OsmSharp.Db.Tiled.Indexes;
using OsmSharp.Db.Tiled.IO;
using OsmSharp.Db.Tiled.Tiles;
using OsmSharp.Streams;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OsmSharp.Db.Tiled
{
    /// <summary>
    /// Represents a database.
    /// </summary>
    public class Database
    {
        private readonly string _path;
        private readonly uint _zoom;
        private const uint ZoomOffset = 2;

        private Dictionary<uint, Dictionary<ulong, Index>> _nodeIndexesCache;
        private Dictionary<uint, Dictionary<ulong, Index>> _wayIndexesCache;

        /// <summary>
        /// Creates a new data based on the given folder.
        /// </summary>
        public Database(string folder, uint zoom = 14)
        {
            // TODO: verify that zoomoffset leads to zoom zero from the given zoom level here.
            // in other words, zoom level has to be exactly dividable by ZoomOffset.
            _path = folder;
            _zoom = zoom;

            _nodeIndexesCache = new Dictionary<uint, Dictionary<ulong, Index>>();
            _wayIndexesCache = new Dictionary<uint, Dictionary<ulong, Index>>();
        }

//        /// <summary>
//        /// Creates a new node.
//        /// </summary>
//        public void CreateNode(Node node)
//        {
//            var tile = DatabaseCommon.FindTileByLocation(_zoom, node.Latitude.Value, node.Longitude.Value);
//            //var index = this.LoadIndex(OsmGeoType.Node, tile);
//            
//            var nodeId = _idGenerator.GenerateNew(OsmGeoType.Node);
//            node.Id = nodeId;
//
//            // write node.
//            DatabaseCommon.AppendToTile(_path, tile, node);
//
//            // recursively update indexes.
//            var mask = tile.BuildMask2();
//            while (tile.Zoom != 0)
//            {
//                tile = tile.ParentTileAt(tile.Zoom - ZoomOffset);
//
//                var index = this.LoadIndex(OsmGeoType.Node, tile, true);
//                index.Add(nodeId, mask);
//                if (tile.Zoom > 0)
//                {
//                    mask = tile.BuildMask2();
//                }
//            }
//
//            this.Flush();
//        }
        
        /// <summary>
        /// Gets the node with given id.
        /// </summary>
        public Node GetNode(long id)
        {
            var tile = new Tile(0, 0, 0);
            var index = LoadIndex(OsmGeoType.Node, tile);

            int mask;
            while (index != null &&
                index.TryGetMask(id, out mask))
            {
                var subTiles = tile.SubTilesForMask2(mask);
                var subTile = subTiles.First();

                if (subTile.Zoom == _zoom)
                { // load data and find node.
                    var stream = DatabaseCommon.LoadTile(_path, OsmGeoType.Node, subTile);
                    if (stream == null)
                    {
                        return null;
                    }
                    using (stream)
                    // using (var uncompressed = new LZ4.LZ4Stream(stream, LZ4.LZ4StreamMode.Decompress))
                    {
                        var source = new OsmSharp.Streams.BinaryOsmStreamSource(stream);
                        while (source.MoveNext(false, true, true))
                        {
                            var current = source.Current();

                            if (current.Id == id)
                            {
                                return current as Node;
                            }
                        }
                    }
                }

                tile = subTile;
                index = LoadIndex(OsmGeoType.Node, tile);
            }

            return null;
        }

        /// <summary>
        /// Gets the way with given id.
        /// </summary>
        public Way GetWay(long id)
        {
            var tile = new Tile(0, 0, 0);
            var index = LoadIndex(OsmGeoType.Way, tile);

            int mask;
            while (index.TryGetMask(id, out mask))
            {
                var subTiles = tile.SubTilesForMask2(mask);
                var subTile = subTiles.First();

                if (subTile.Zoom == _zoom)
                { // load data and find node.
                    var stream = DatabaseCommon.LoadTile(_path, OsmGeoType.Way, subTile);
                    if (stream == null)
                    {
                        return null;
                    }
                    using (stream)
                    using (var uncompressed = new LZ4.LZ4Stream(stream, LZ4.LZ4StreamMode.Decompress))
                    {
                        var source = new OsmSharp.Streams.BinaryOsmStreamSource(uncompressed);
                        while (source.MoveNext(true, false, true))
                        {
                            var current = source.Current();

                            if (current.Id == id)
                            {
                                return current as Way;
                            }
                        }
                    }
                }

                tile = subTile;
                index = LoadIndex(OsmGeoType.Way, tile);
            }

            return null;
        }

        /// <summary>
        /// Gets all the relevant tiles.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Tile> GetTiles()
        {
            var basePath = FileSystemFacade.FileSystem.Combine(_path, _zoom.ToInvariantString());
            if (!FileSystemFacade.FileSystem.DirectoryExists(basePath))
            {
                yield break;
            }
            foreach(var xDir in FileSystemFacade.FileSystem.EnumerateDirectories(
                basePath))
            {
                var xDirName = FileSystemFacade.FileSystem.DirectoryName(xDir);
                uint x;
                if (!uint.TryParse(xDirName, out x))
                {
                    continue;
                }

                foreach (var tile in FileSystemFacade.FileSystem.EnumerateFiles(xDir, "*.nodes.osm.bin"))
                {
                    var tileName = FileSystemFacade.FileSystem.FileName(tile);

                    uint y;
                    if (!uint.TryParse(tileName.Substring(0,
                        tileName.IndexOf('.')), out y))
                    {
                        continue;
                    }

                    yield return new Tile(x, y, _zoom);
                }
            }
        }

        /// <summary>
        /// Gets a complete tile with complete ways and complete first level relations.
        /// </summary>
        public bool GetCompleteTile(Tile tile, OsmStreamTarget target)
        {
            if (tile.Zoom != _zoom) { throw new ArgumentException("Tile not a the db zoom level."); }

            var nodes = new HashSet<long>();
            using (var stream = DatabaseCommon.LoadTile(_path, OsmGeoType.Node, tile))
            //using (var uncompressed = new LZ4.LZ4Stream(stream, LZ4.LZ4StreamMode.Decompress))
            {
                var source = new OsmSharp.Streams.BinaryOsmStreamSource(stream);
                while (source.MoveNext(false, true, true))
                {
                    var current = source.Current();

                    var n = current as Node;
                    var nodeId = n.Id.Value;

                    nodes.Add(n.Id.Value);
                }
            }

            var sortedIds = new SortedSet<long>();
            using (var stream = DatabaseCommon.LoadTile(_path, OsmGeoType.Way, tile))
            {
                if (stream != null)
                {
                    //using (var uncompressed = new LZ4.LZ4Stream(stream, LZ4.LZ4StreamMode.Decompress))
                    //{
                        var source = new OsmSharp.Streams.BinaryOsmStreamSource(stream);
                        while (source.MoveNext(true, false, true))
                        {
                            var current = source.Current();

                            var w = current as Way;

                            if (w.Nodes == null)
                            {
                                continue;
                            }

                            foreach (var n in w.Nodes)
                            {
                                if (!nodes.Contains(n))
                                {
                                    sortedIds.Add(n);
                                }
                            }
                        }
                    //}
                }
            }

            var hasData = false;
            var sortedIdsEnumerator = sortedIds.GetEnumerator();
            var hasNextSortedId = sortedIdsEnumerator.MoveNext();
            using (var stream = DatabaseCommon.LoadTile(_path, OsmGeoType.Node, tile))
            {
                //using (var uncompressed = new LZ4.LZ4Stream(stream, LZ4.LZ4StreamMode.Decompress))
                //{
                    var source = new OsmSharp.Streams.BinaryOsmStreamSource(stream);
                    while (source.MoveNext(false, true, true))
                    {
                        var current = source.Current();

                        var n = current as Node;
                        var nodeId = n.Id.Value;

                        // write the extra nodes until the curent node is next.
                        while (hasNextSortedId &&
                            nodeId > sortedIdsEnumerator.Current)
                        {
                            var node = this.GetNode(sortedIdsEnumerator.Current);

                            target.AddNode(node);

                            hasNextSortedId = sortedIdsEnumerator.MoveNext();
                        }

                        hasData = true;
                        target.AddNode(n);
                    }
                //}
            }

            using (var stream = DatabaseCommon.LoadTile(_path, OsmGeoType.Way, tile))
            {
                if (stream != null)
                {
                    //using (var uncompressed = new LZ4.LZ4Stream(stream, LZ4.LZ4StreamMode.Decompress))
                    //{
                        var source = new OsmSharp.Streams.BinaryOsmStreamSource(stream);
                        while (source.MoveNext(true, false, true))
                        {
                            var current = source.Current();

                            var w = current as Way;

                            target.AddWay(w);
                        }
                    //}
                }
            }

            using (var stream = DatabaseCommon.LoadTile(_path, OsmGeoType.Relation, tile))
            {
                if (stream != null)
                {
                    //using (var uncompressed = new LZ4.LZ4Stream(stream, LZ4.LZ4StreamMode.Decompress))
                    //{
                        var source = new OsmSharp.Streams.BinaryOsmStreamSource(stream);
                        while (source.MoveNext(true, true, false))
                        {
                            var current = source.Current();

                            var r = current as Relation;

                            target.AddRelation(r);
                        }
                    //}
                }
            }

            return hasData;
        }

        /// <summary>
        /// Loads the index for the given type and tile.
        /// </summary>
        private Index LoadIndex(OsmGeoType type, Tile tile, bool create = false)
        {
            if (type == OsmGeoType.Node)
            {
                Dictionary<ulong, Index> cached;
                if (!_nodeIndexesCache.TryGetValue(tile.Zoom, out cached))
                {
                    cached = new Dictionary<ulong, Index>();
                    _nodeIndexesCache[tile.Zoom] = cached;
                }
                Index index;
                if (cached.TryGetValue(tile.LocalId, out index))
                {
                    return index;
                }

                index = DatabaseCommon.LoadIndex(_path, tile, type);
                if (create && index == null)
                {
                    index = new Index();
                }
                cached[tile.LocalId] = index;
                return index;
            }
            else
            {
                Dictionary<ulong, Index> cached;
                if (!_wayIndexesCache.TryGetValue(tile.Zoom, out cached))
                {
                    cached = new Dictionary<ulong, Index>();
                    _wayIndexesCache[tile.Zoom] = cached;
                }
                Index index;
                if (cached.TryGetValue(tile.LocalId, out index))
                {
                    return index;
                }

                index = DatabaseCommon.LoadIndex(_path, tile, type);
                if (create && index == null)
                {
                    index = new Index();
                }
                cached[tile.LocalId] = index;
                return index;
            }
        }

        /// <summary>
        /// Fluses all in-memory data to disk.
        /// </summary>
        public void Flush()
        {
            foreach (var zoomCache in _nodeIndexesCache)
            {
                foreach (var tileCache in zoomCache.Value)
                {
                    if (tileCache.Value.IsDirty)
                    {
                        var tile = Tile.FromLocalId(zoomCache.Key, tileCache.Key);
                        DatabaseCommon.SaveIndex(_path, tile, OsmGeoType.Node, tileCache.Value);
                    }
                }
            }
            
            foreach (var zoomCache in _wayIndexesCache)
            {
                foreach (var tileCache in zoomCache.Value)
                {
                    if (tileCache.Value.IsDirty)
                    {
                        var tile = Tile.FromLocalId(zoomCache.Key, tileCache.Key);
                        DatabaseCommon.SaveIndex(_path, tile, OsmGeoType.Way, tileCache.Value);
                    }
                }
            }
            
            // foreach (var zoomCache in _relationIndexesCache)
            // {
            //     foreach (var tileCache in zoomCache.Value)
            //     {
            //         if (tileCache.Value.IsDirty)
            //         {
            //             var tile = Tile.FromLocalId(zoomCache.Key, tileCache.Key);
            //             DatabaseCommon.SaveIndex(_path, tile, OsmGeoType.Way, tileCache.Value);
            //         }
            //     }
            // }
        }
    }
}