using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OsmSharp;
using OsmSharp.Db.Tiled.OsmTiled;

namespace RouteableTiles.API
{
    internal static class OsmDbExtensions
    {
        internal static async Task<IEnumerable<OsmGeo>> GetRouteableTile(this OsmTiledDbBase db, (uint x, uint y) tile, 
            Func<OsmGeo, bool> isRelevant)
        {
            var result = new List<OsmGeo>();
            
            var nodesInTile = new Dictionary<long, Node>();
            var nodesToInclude = new SortedDictionary<long, Node>();
            var waysToInclude = new SortedDictionary<long, Way>();
            
            var osmGeos = await db.Get(tile);
            foreach (var osmGeo in osmGeos)
            {
                if (osmGeo is Node node)
                {
                    if (node?.Id == null) continue;
                    nodesInTile.Add(node.Id.Value, node);
                }
                else if (osmGeo is Way w)
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
                    if (!isRelevant(w)) continue;

                    waysToInclude[w.Id.Value] = w;

                    if (first > 0) first--;
                    if (last < w.Nodes.Length - 1) last++;
                    for (var n = first; n < last + 1; n++)
                    {
                        var nodeId = w.Nodes[n];

                        // get node from the nodes in the tile.
                        if (nodesInTile.TryGetValue(nodeId, out var wayNode))
                        {
                            // node already there.
                            nodesToInclude[nodeId] = wayNode;
                            continue;
                        }

                        // node is not in the tile, get it from the db if it's not included already..
                        if (nodesToInclude.ContainsKey(nodeId)) continue;
                    
                        // not not yet there, get it.
                        wayNode = await db.Get(OsmGeoType.Node, nodeId) as Node;
                        if (wayNode != null) nodesToInclude[nodeId] = wayNode;
                    }
                }
                else
                {
                    if (result.Count == 0)
                    {
                        // return all the nodes.
                        result.AddRange(nodesToInclude.Values);

                        // returns all the ways that have at least one node.
                        foreach (var wayToInclude in waysToInclude.Values)
                        {
                            // trim nodes.
                            var trimmedNodes = new List<long>();
                            foreach (var n in wayToInclude.Nodes)
                            {
                                if (!nodesToInclude.TryGetValue(n, out _)) continue;

                                trimmedNodes.Add(n);
                            }

                            wayToInclude.Nodes = trimmedNodes.ToArray();

                            if (wayToInclude.Nodes.Length == 0) continue;

                            result.Add(wayToInclude);
                        }
                    }

                    if (osmGeo is Relation r)
                    {                
                        if (r.Members == null) continue;
                        if (!isRelevant(r)) continue;

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

                        result.Add(r);
                    }
                }
            }

            return result;
        }
    }
}