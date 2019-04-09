using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OsmSharp;
using OsmSharp.Streams;
using RouteableTiles.Build.Indexes;
using RouteableTiles.Collections;
using RouteableTiles.IO;
using RouteableTiles.Tiles;
using Serilog;

namespace RouteableTiles
{
    public class Database
    {
        private readonly string _path;
        private readonly bool _compressed;
        private readonly uint _zoom;
        private const uint ZoomOffset = 2;
        private const bool _mapped = true;

        private readonly ConcurrentDictionary<uint, LRUCache<ulong, Index>> _nodeIndexesCache;
        private readonly ConcurrentDictionary<uint, LRUCache<ulong, Index>> _wayIndexesCache;

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

            _nodeIndexesCache = new ConcurrentDictionary<uint, LRUCache<ulong, Index>>();
            _wayIndexesCache = new ConcurrentDictionary<uint, LRUCache<ulong, Index>>();
        }

        /// <summary>
        /// Gets the zoom.
        /// </summary>
        public uint Zoom => _zoom;
        
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
                        Log.Warning($"Could not find subtile, it should be there: {subTile}");
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
                    cached = new LRUCache<ulong, Index>(10);
                    _nodeIndexesCache[tile.Zoom] = cached;
                }

                if (cached.TryGetValue(tile.LocalId, out var index))
                {
                    return index;
                }

                index = DatabaseCommon.LoadIndex(_path, tile, type, _mapped);
                if (create && index == null)
                {
                    index = new Index();
                }
                cached.Add(tile.LocalId, index);
                return index;
            }
            else
            {
                if (!_wayIndexesCache.TryGetValue(tile.Zoom, out var cached))
                {
                    cached = new LRUCache<ulong, Index>(10);
                    _wayIndexesCache[tile.Zoom] = cached;
                }

                if (cached.TryGetValue(tile.LocalId, out var index))
                {
                    return index;
                }

                index = DatabaseCommon.LoadIndex(_path, tile, type, _mapped);
                if (create && index == null)
                {
                    index = new Index();
                }
                cached.Add(tile.LocalId, index);
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
                if (!uint.TryParse(xDirName, out var x))
                {
                    continue;
                }

                foreach (var tile in FileSystemFacade.FileSystem.EnumerateFiles(xDir, mask))
                {
                    var tileName = FileSystemFacade.FileSystem.FileName(tile);

                    if (!uint.TryParse(tileName.Substring(0,
                        tileName.IndexOf('.')), out var y))
                    {
                        continue;
                    }

                    yield return new Tile(x, y, _zoom);
                }
            }
        }

        /// <summary>
        /// Gets the a routeable tile for the given tile.
        /// </summary>
        /// <param name="tile">The tile.</param>
        /// <returns>An enumerable with all data that should be in a routeable tile.</returns>
        public IEnumerable<OsmGeo> GetRouteableTile(Tile tile)
        {
            if (tile.Zoom != _zoom) { return null; }

            var stream = DatabaseCommon.LoadTile(_path, OsmGeoType.Node, tile, _compressed);
            if (stream == null) return null;

            return this.GetRouteableTile(tile, stream);
        }
        
        /// <summary>
        /// Gets a complete tile with complete ways and complete first level relations.
        /// </summary>
        internal IEnumerable<OsmGeo> GetRouteableTile(Tile tile, Stream stream)
        {
            // build a hashset of all nodes in the tile.
            var nodesInTile = new Dictionary<long, Node>();
            using (stream)
            {
                if (stream == null)
                {
                    yield break;
                }
                var source = new OsmSharp.Streams.BinaryOsmStreamSource(stream);
                while (source.MoveNext(false, true, true))
                {
                    var current = source.Current();

                    if (!(current is Node n)) continue;
                    if (n.Id == null) continue;
                    
                    nodesInTile.Add(n.Id.Value, n);
                }
            }

            // go over all ways and also include all nodes between the first/last node in the tile.
            // also include one node before or after the first/last node in the tile.
            var nodesToInclude = new SortedDictionary<long, Node>();
            using (stream = DatabaseCommon.LoadTile(_path, OsmGeoType.Way, tile, _compressed))
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

                        var first = int.MaxValue;
                        var last = -1;
                        for (var n = 0; n < w.Nodes.Length; n++)
                        {
                            var nodeId = w.Nodes[n];
                            if (!nodesInTile.ContainsKey(nodeId)) continue;

                            if (n < first) first = n;
                            if (n > last) last = n;
                        }

                        if (first == int.MaxValue) continue;

                        if (first > 0) first--;
                        if (last < w.Nodes.Length - 1) last++;
                        for (var n = first; n < last; n++)
                        {
                            var nodeId = w.Nodes[n];
                            
                            // get node from the nodes in the tile.
                            if (nodesInTile.TryGetValue(nodeId, out var node))
                            { // node already there.
                                nodesToInclude[nodeId] = node;
                                continue;
                            }
                            
                            // node is not in the tile, get it from the db if it's not included already..
                            if (!nodesToInclude.ContainsKey(nodeId))
                            { // not not yet there, get it.
                                node = this.GetNode(nodeId);
                                nodesToInclude[nodeId] = node;
                            }
                        }
                    }
                }
            }

            var hasData = nodesToInclude.Count > 0;
            if (!hasData)
            {
                yield break;
            }
            
            // return all the nodes.
            foreach (var node in nodesToInclude.Values)
            {
                yield return node;
            }

            // returns all the ways that have at least one node.
            using (stream = DatabaseCommon.LoadTile(_path, OsmGeoType.Way, tile, _compressed))
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
                            if (!nodesToInclude.TryGetValue(n, out _)) continue;
                            
                            trimmedNodes.Add(n);
                        }
                        w.Nodes = trimmedNodes.ToArray();
                        
                        if (w.Nodes.Length == 0) continue;

                        yield return w;
                    }
                }
            }

            // return all relations.
            using (stream = DatabaseCommon.LoadTile(_path, OsmGeoType.Relation, tile, _compressed))
            {
                if (stream != null)
                {
                    var source = new OsmSharp.Streams.BinaryOsmStreamSource(stream);
                    while (source.MoveNext(true, true, false))
                    {
                        var current = source.Current();

                        var r = current as Relation;

                        yield return r;
                    }
                }
            }
        }
    }
}