using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using OsmSharp;
using RouteableTiles.IO.JsonLD;
using RouteableTiles.Tiles;

namespace RouteableTiles.API.Controllers
{
    [Route("/")]
    [ApiController]
    public class TilesController : ControllerBase
    {
        [HttpGet("{z}/{x}/{y}/")]
        public object Get(uint z, uint x, uint y)
        {
            var db = DatabaseInstance.Default;
            
            if (db == null) return NotFound();
            if (db.Zoom != z) return NotFound();

            var tile = new Tile(x, y, z);
            var data = db.GetRouteableTile(tile);
            if (data == null) return NotFound();
            
            Response.Headers[HeaderNames.CacheControl] = "public,max-age=" + 60 * 60;
            
            return data;
        }
    }
}