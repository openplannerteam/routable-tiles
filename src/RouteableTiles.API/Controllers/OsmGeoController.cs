using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OsmSharp;

namespace RouteableTiles.API.Controllers
{
    [Route("/")]
    [ApiController]
    public class OsmGeoController
    {
        [HttpGet("node/{id}/tiles")]
        public async Task<object> GetNode(long id)
        {
            var db = DatabaseInstance.Default;

            var node = db.Latest.Get(OsmGeoType.Node, id);

            return node;
        }
        
        [HttpGet("way/{id}")]
        public async Task<object> GetWay(long id)
        {
            var db = DatabaseInstance.Default;

            var way = db.Latest.Get(OsmGeoType.Way, id);

            return way;
        }
        
        [HttpGet("relation/{id}")]
        public async Task<object> GetRelation(long id)
        {
            var db = DatabaseInstance.Default;

            var relation = db.Latest.Get(OsmGeoType.Relation, id);

            return relation;
        }
    }
}