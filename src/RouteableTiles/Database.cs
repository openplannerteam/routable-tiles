using System;
using System.Collections.Generic;
using System.Linq;
using OsmSharp;
using OsmSharp.Streams;
using RouteableTiles.Build.Indexes;
using RouteableTiles.IO;
using RouteableTiles.Tiles;

namespace RouteableTiles
{
    public class Database
    {
        private readonly string _path;
        private readonly bool _compressed = false;
        private readonly uint _zoom;
        private const uint ZoomOffset = 2;

        private readonly Dictionary<uint, Dictionary<ulong, Index>> _nodeIndexesCache;
        private readonly Dictionary<uint, Dictionary<ulong, Index>> _wayIndexesCache;

        /// <summary>
        /// Creates a new data based on the given folder.
        /// </summary>
        public Database(string folder, uint zoom = 12, bool compressed = true)
        {
            // TODO: verify that zoom offset leads to zoom zero from the given zoom level here.
            // in other words, zoom level has to be exactly dividable by ZoomOffset.
            _path = folder;
            _compressed = compressed;
            _zoom = zoom;

            _nodeIndexesCache = new Dictionary<uint, Dictionary<ulong, Index>>();
            _wayIndexesCache = new Dictionary<uint, Dictionary<ulong, Index>>();
        }
        
        /// <summary>
        /// Gets the node with given id.
        /// </summary>
        public Node GetNode(long id)
        {
            var tile = new Tile(0, 0, 0);
            var index = LoadIndex(OsmGeoType.Node, tile);

            while (index != null &&
                   index.TryGetMask(id, out var mask))
            {
                var subTiles = tile.SubTilesForMask2(mask);
                var subTile = subTiles.First();

                if (subTile.Zoom == _zoom)
                { // load data and find node.
                    var stream = DatabaseCommon.LoadTile(_path, OsmGeoType.Node, subTile, _compressed);
                    if (stream == null)
                    {
                        return null;
                    }
                    using (stream)
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
        /// Loads the index for the given type and tile.
        /// </summary>
        private Index LoadIndex(OsmGeoType type, Tile tile, bool create = false)
        {
            if (type == OsmGeoType.Node)
            {
                if (!_nodeIndexesCache.TryGetValue(tile.Zoom, out var cached))
                {
                    cached = new Dictionary<ulong, Index>();
                    _nodeIndexesCache[tile.Zoom] = cached;
                }

                if (cached.TryGetValue(tile.LocalId, out var index))
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
                if (!_wayIndexesCache.TryGetValue(tile.Zoom, out var cached))
                {
                    cached = new Dictionary<ulong, Index>();
                    _wayIndexesCache[tile.Zoom] = cached;
                }

                if (cached.TryGetValue(tile.LocalId, out var index))
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
            var mask = "*.nodes.osm.bin";
            if (_compressed) mask = mask + ".zip";
            foreach(var xDir in FileSystemFacade.FileSystem.EnumerateDirectories(
                basePath))
            {
                var xDirName = FileSystemFacade.FileSystem.DirectoryName(xDir);
                uint x;
                if (!uint.TryParse(xDirName, out x))
                {
                    continue;
                }

                foreach (var tile in FileSystemFacade.FileSystem.EnumerateFiles(xDir, mask))
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
        public bool GetRoutableTile(Tile tile, OsmStreamTarget target)
        {
            if (tile.Zoom != _zoom) { throw new ArgumentException("Tile not a the db zoom level."); }

            var nodes = new Dictionary<long, Node>();
            using (var stream = DatabaseCommon.LoadTile(_path, OsmGeoType.Node, tile, _compressed))
            {
                var source = new OsmSharp.Streams.BinaryOsmStreamSource(stream);
                while (source.MoveNext(false, true, true))
                {
                    var current = source.Current();

                    if (!(current is Node n)) continue;
                    if (n.Id == null) continue;
                    
                    var nodeId = n.Id.Value;
                    nodes.Add(n.Id.Value, n);
                }
            }

            var sortedIds = new SortedDictionary<long, Node>();
            using (var stream = DatabaseCommon.LoadTile(_path, OsmGeoType.Way, tile, _compressed))
            {
                if (stream != null)
                {
                    var source = new OsmSharp.Streams.BinaryOsmStreamSource(stream);
                    while (source.MoveNext(true, false, true))
                    {
                        var current = source.Current();

                        var w = current as Way;
                        if (w?.Nodes == null)
                        {
                            continue;
                        }

                        for (var n = 0; n < w.Nodes.Length; n++)
                        {
                            var nodeId = w.Nodes[n];
                            if (!nodes.TryGetValue(nodeId, out var node)) continue;
                            sortedIds[nodeId] = node;
                            
                            if (n > 0)
                            {
                                nodeId = w.Nodes[n - 1];
                                if (!nodes.TryGetValue(nodeId, out node))
                                {
                                    if (!sortedIds.TryGetValue(nodeId, out node))
                                    {
                                        node = this.GetNode(nodeId);
                                        sortedIds[nodeId] = node;
                                    }
                                }
                                sortedIds[nodeId] = node;
                            }
                            if (n < w.Nodes.Length - 1)
                            {
                                nodeId = w.Nodes[n + 1];
                                if (!nodes.TryGetValue(nodeId, out node))
                                {
                                    if (!sortedIds.TryGetValue(nodeId, out node))
                                    {
                                        node = this.GetNode(nodeId);
                                        sortedIds[nodeId] = node;
                                    }
                                }
                                sortedIds[nodeId] = node;
                            }
                        }
                    }
                }
            }

            var hasData = sortedIds.Count > 0;
            foreach (var node in sortedIds.Values)
            {
                target.AddNode(node);
            }

            using (var stream = DatabaseCommon.LoadTile(_path, OsmGeoType.Way, tile, _compressed))
            {
                if (stream != null)
                {
                    var source = new OsmSharp.Streams.BinaryOsmStreamSource(stream);
                    while (source.MoveNext(true, false, true))
                    {
                        var current = source.Current();

                        var w = current as Way;
                        if (w == null) continue;
                        
                        // trim nodes.
                        var trimmedNodes = new List<long>();
                        foreach (var n in w.Nodes)
                        {
                            if (!sortedIds.TryGetValue(n, out _)) continue;
                            
                            trimmedNodes.Add(n);
                        }
                        w.Nodes = trimmedNodes.ToArray();

                        target.AddWay(w);
                    }
                }
            }

            using (var stream = DatabaseCommon.LoadTile(_path, OsmGeoType.Relation, tile, _compressed))
            {
                if (stream != null)
                {
                    var source = new OsmSharp.Streams.BinaryOsmStreamSource(stream);
                    while (source.MoveNext(true, true, false))
                    {
                        var current = source.Current();

                        var r = current as Relation;

                        target.AddRelation(r);
                    }
                }
            }

            return hasData;
        }
    }
}