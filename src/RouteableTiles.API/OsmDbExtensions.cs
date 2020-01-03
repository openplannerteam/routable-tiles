using System.Collections.Generic;
using OsmSharp;
using OsmSharp.Db.Tiled;
using OsmSharp.Db.Tiled.Snapshots;
using RouteableTiles.IO.JsonLD.Tiles;

namespace RouteableTiles.API
{
    internal static class OsmDbExtensions
    {
        internal static IEnumerable<OsmGeo> GetRouteableTile(this SnapshotDb db, Tile tile)
        {
            var nodes = db.GetTile(new OsmSharp.Db.Tiled.Tiles.Tile(tile.X, tile.Y, tile.Zoom), OsmGeoType.Node);
            var ways = db.GetTile(new OsmSharp.Db.Tiled.Tiles.Tile(tile.X, tile.Y, tile.Zoom), OsmGeoType.Way);
            var relations = db.GetTile(new OsmSharp.Db.Tiled.Tiles.Tile(tile.X, tile.Y, tile.Zoom), OsmGeoType.Relation);
            
            // build a hashset of all nodes in the tile.
            var nodesInTile = new Dictionary<long, Node>();
            foreach (var current in nodes)
            {
                var n = current as Node;
                if (n?.Id == null) continue;
                    
                nodesInTile.Add(n.Id.Value, n);
            }
            
            // go over all ways and also include all nodes between the first/last node in the tile.
            // also include one node before or after the first/last node in the tile.
            var nodesToInclude = new SortedDictionary<long, Node>();
            foreach (var current in ways)
            {
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
                for (var n = first; n < last + 1; n++)
                {
                    var nodeId = w.Nodes[n];

                    // get node from the nodes in the tile.
                    if (nodesInTile.TryGetValue(nodeId, out var node))
                    {
                        // node already there.
                        nodesToInclude[nodeId] = node;
                        continue;
                    }

                    // node is not in the tile, get it from the db if it's not included already..
                    if (nodesToInclude.ContainsKey(nodeId)) continue;
                    
                    // not not yet there, get it.
                    node = db.Get(OsmGeoType.Node, nodeId) as Node;
                    if (node != null) nodesToInclude[nodeId] = node;
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
            foreach (var current in ways)
            {
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

            // return all relations.
            foreach (var current in relations)
            {
                var r = current as Relation;
                
                yield return r;
            }
        }
    }
}