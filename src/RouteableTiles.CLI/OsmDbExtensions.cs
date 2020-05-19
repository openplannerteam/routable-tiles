using System;
using System.Collections.Generic;
using System.Linq;
using OsmSharp;
using OsmSharp.Db.Tiled.OsmTiled;
using RouteableTiles.IO.JsonLD;

namespace RouteableTiles.CLI
{
    internal static class OsmDbExtensions
    {
        internal static IEnumerable<OsmGeo> GetRouteableTile(this OsmTiledDbBase db, (uint x, uint y) tile, 
            Func<OsmGeo, bool> isRelevant, byte[]? buffer = null)
        {
            return db.Get(new[] {tile}).Select(x => x.osmGeo).GetRouteableTile(
                isRelevant, keys => db.Get(keys, buffer).Select(x => x.osmGeo));
        }
    }
}