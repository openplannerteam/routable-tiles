using System.Collections.Generic;
using System.Linq;
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
            var nodes = db.GetNodesInTile(tile).Where(n => n.Longitude != null && 
                                                           n.Latitude != null && 
                                                           tile.Overlaps(n.Latitude.Value, n.Longitude.Value));
            var ways = db.GetWaysInTile(tile);
            var relations = db.GetRelationsInTile(tile);
            
            // build a hashset of all nodes in the tile.
            var nodesInTile = new Dictionary<long, Node>();
            foreach (var current in nodes)
            {
                var n = current as Node;
                if (n?.Id == null) continue;
                    
                nodesInTile.Add(n.Id.Value, n);
            }
            
            // go over all ways and keep only those ways with at least one node in the nodes hashset.
            // also include one node before or after the first/last node in the tile.
            var nodesToInclude = new SortedDictionary<long, Node>();
            var waysToInclude = new SortedDictionary<long, Way>();
            foreach (var w in ways)
            {
                if (w?.Id == null) continue;
                if (w?.Nodes == null) continue;
                
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

                waysToInclude[w.Id.Value] = w;

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
            foreach (var w in waysToInclude.Values)
            {
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
            foreach (var r in relations)
            {
                if (r.Members == null) continue;

                var include = false;
                foreach (var m in r.Members)
                {
                    switch (m.Type)
                    {
                        case OsmGeoType.Node:
                            if (nodesInTile.ContainsKey(m.Id)) include = true;
                            break;
                        case OsmGeoType.Way:
                            if (waysToInclude.ContainsKey(m.Id)) include = true;
                            break;       
                    }
                    if (include) break;
                }
                if (!include) continue;

                yield return r;
            }
        }

        internal static IEnumerable<Node> GetNodesInTile(this SnapshotDb db, Tile tile)
        {
            var tiles = tile.GetTilesAt(db.Zoom);
            
            foreach (var t in tiles)
            foreach (var osmGeo in db.GetTile(t.X, t.Y, OsmGeoType.Node))
            {
                var node = osmGeo as Node;
                if (node == null) continue;

                yield return node;
            }
        }

        internal static IEnumerable<Way> GetWaysInTile(this SnapshotDb db, Tile tile)
        {
            var ids = new HashSet<long>();
            var tiles = tile.GetTilesAt(db.Zoom);
            
            foreach (var t in tiles)
            foreach (var osmGeo in db.GetTile(t.X, t.Y, OsmGeoType.Way))
            {
                var way = osmGeo as Way;
                if (way?.Id == null) continue;
                if (!ids.Add(way.Id.Value)) continue;

                yield return way;
            }
        }

        internal static IEnumerable<Relation> GetRelationsInTile(this SnapshotDb db, Tile tile)
        {
            var ids = new HashSet<long>();
            var tiles = tile.GetTilesAt(db.Zoom);
            
            foreach (var t in tiles)
            foreach (var osmGeo in db.GetTile(t.X, t.Y, OsmGeoType.Relation))
            {
                var relation = osmGeo as Relation;
                if (relation?.Id == null) continue;
                if (!ids.Add(relation.Id.Value)) continue;

                yield return relation;
            }
        }
    }
}