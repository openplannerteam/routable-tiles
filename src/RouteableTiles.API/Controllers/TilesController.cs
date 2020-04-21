using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using OsmSharp;
using RouteableTiles.API.Results;
using RouteableTiles.IO.JsonLD;
using RouteableTiles.IO.JsonLD.Semantics;
using RouteableTiles.IO.JsonLD.Tiles;
using Serilog;

namespace RouteableTiles.API.Controllers
{
    [Route("/")]
    [ApiController]
    public class TilesController : ControllerBase
    {
        [HttpGet("{z}/{x}/{y}/")]
        public async Task<object> Get(uint z, uint x, uint y)
        {
            var db = DatabaseInstance.Default;
            
            if (db == null) return NotFound();
            if (db.Latest == null) return NotFound();
            if (z != db.Latest.Zoom) return NotFound();

            var tile = new Tile(x, y, z);
            var data = await db.Latest.GetRouteableTile((x, y), (ts) => ts.IsRelevant(JsonLDOutputFormatter.MappingKeys, JsonLDOutputFormatter.Mapping));
            
            Response.Headers[HeaderNames.CacheControl] = "public,max-age=" + 60 * 60;
            
            return new TileResponse()
            {
                Data = data,
                Tile = tile
            };
        }
    }
}