using System;
using System.Collections.Generic;
using OsmSharp;
using OsmSharp.Logging;

namespace RouteableTiles.IO.JsonLD
{
    public static class OsmGeoExtensions
    {
        public static IEnumerable<OsmGeo> GetRouteableTile(this IEnumerable<OsmGeo> tileData,
            Func<OsmGeo, bool>? isRelevant = null, Func<OsmGeoKey, OsmGeo?>? getOsmGeo = null)
        {
            var result = new List<OsmGeo>();
            
            var nodesInTile = new Dictionary<long, Node>();
            var nodesToInclude = new SortedDictionary<long, Node>();
            var waysToInclude = new SortedDictionary<long, Way>();
            
            foreach (var osmGeo in tileData)
            {
                if (osmGeo is Node node)
                {
                    if (node?.Id == null) continue;

                    if (isRelevant != null && isRelevant(node))
                    {
                        nodesToInclude[node.Id.Value] = node;
                    }
                    nodesInTile[node.Id.Value] = node;
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
                    if (isRelevant != null && !isRelevant(w)) continue;

                    waysToInclude[w.Id.Value] = w;

                    if (first > 0) first--;
                    if (last < w.Nodes.Length - 1) last++;
                    for (var n = first; n < last + 1; n++)
                    {
                        var nodeId = w.Nodes[n];

                        // get node from the nodes in the tile.
                        if (nodesInTile.TryGetValue(nodeId, out Node? wayNode))
                        {
                            // node already there.
                            nodesToInclude[nodeId] = wayNode;
                            continue;
                        }

                        // node is not in the tile, get it from the db if it's not included already..
                        if (nodesToInclude.ContainsKey(nodeId)) continue;
                    
                        // not not yet there, get it.
                        wayNode = getOsmGeo?.Invoke(new OsmGeoKey(OsmGeoType.Node, nodeId)) as Node;
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
                        if (isRelevant != null && !isRelevant(r)) continue;

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