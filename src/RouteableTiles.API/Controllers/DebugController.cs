using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using OsmSharp;
using OsmSharp.Db.Tiled.OsmTiled;
using RouteableTiles.API.Responses;
using RouteableTiles.IO.JsonLD.Tiles;

namespace RouteableTiles.API.Controllers
{
    public class DebugController : ControllerBase
    {
        [HttpGet("debug/{z}/{x}/{y}.osm")]
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
        
        [HttpGet("debug/node/{id}")]
        public Node GetNode(long id)
        {
            var db = DatabaseInstance.Default;

            var osmGeo = db.Latest.Get(OsmGeoType.Node, id);
            if (!(osmGeo is Node node)) throw new InvalidDataException("Expected a node when a node was requested.");
            return node;
        }
        
        [HttpGet("debug/way/{id}")]
        public Way GetWay(long id)
        {
            var db = DatabaseInstance.Default;

            var osmGeo = db.Latest.Get(OsmGeoType.Way, id);
            if (!(osmGeo is Way way)) throw new InvalidDataException("Expected a way when a way was requested.");
            return way;
        }
        
        [HttpGet("debug/relation/{id}")]
        public Relation GetRelation(long id)
        {
            var db = DatabaseInstance.Default;

            var osmGeo = db.Latest.Get(OsmGeoType.Relation, id);
            if (!(osmGeo is Relation relation)) throw new InvalidDataException("Expected a relation when a relation was requested.");
            return relation;
        }
    }
}