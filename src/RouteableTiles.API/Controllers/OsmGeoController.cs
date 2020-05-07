using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OsmSharp;
using OsmSharp.Db.Tiled.OsmTiled;

namespace RouteableTiles.API.Controllers
{
    [Route("/")]
    [ApiController]
    public class OsmGeoController
    {
        [HttpGet("node/{id}")]
        public Node GetNode(long id)
        {
            var db = DatabaseInstance.Default;

            var osmGeo = db.Latest.Get(OsmGeoType.Node, id);
            if (!(osmGeo is Node node)) throw new InvalidDataException("Expected a node when a node was requested.");
            return node;
        }
        
        [HttpGet("way/{id}")]
        public Way GetWay(long id)
        {
            var db = DatabaseInstance.Default;

            var osmGeo = db.Latest.Get(OsmGeoType.Way, id);
            if (!(osmGeo is Way way)) throw new InvalidDataException("Expected a way when a way was requested.");
            return way;
        }
        
        [HttpGet("relation/{id}")]
        public async Task<object> GetRelation(long id)
        {
            var db = DatabaseInstance.Default;

            var osmGeo = db.Latest.Get(OsmGeoType.Relation, id);
            if (!(osmGeo is Relation relation)) throw new InvalidDataException("Expected a relation when a relation was requested.");
            return relation;
        }
    }
}