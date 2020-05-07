using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using RouteableTiles.API.Responses;
using RouteableTiles.IO.JsonLD.Semantics;
using RouteableTiles.IO.JsonLD.Tiles;
using OsmSharp.Db.Tiled.OsmTiled;

namespace RouteableTiles.API.Controllers
{
    [Route("/")]
    [ApiController]
    public class TilesController : ControllerBase
    {
        [HttpGet("{z}/{x}/{y}/")]
        public object GetJsonLD(uint z, uint x, uint y)
        {
            var db = DatabaseInstance.Default;
            
            if (db == null) return NotFound();
            if (db.Latest == null) return NotFound();
            if (z != db.Latest.Zoom) return NotFound();

            var tile = new Tile(x, y, z);
            var data = db.Latest.GetRouteableTile((x, y), (ts) => ts.IsRelevant(
                JsonLDTileResponseFormatter.MappingKeys ?? TagMapper.DefaultMappingKeys, 
                JsonLDTileResponseFormatter.Mapping ?? TagMapper.DefaultMappingConfigs));
            
            Response.Headers[HeaderNames.CacheControl] = "public,max-age=" + 60;

            return new OsmTileResponse(data, tile);
        }

        [HttpGet("{z}/{x}/{y}.osm")]
        public object GetOsmXml(uint z, uint x, uint y)
        {
            var db = DatabaseInstance.Default;
            
            if (db == null) return NotFound();
            if (db.Latest == null) return NotFound();
            if (z != db.Latest.Zoom) return NotFound();

            var tile = new Tile(x, y, z);
            var data = db.Latest.Get((x, y), true, false);
            
            Response.Headers[HeaderNames.CacheControl] = "public,max-age=" + 60;

            // TODO: handle this properly, what do we do when application/xml was not requested?
            // force an xml response.
            Request.Headers[HeaderNames.Accept] = "application/xml";

            return new OsmTileResponse(data, tile);
        }
    }
}