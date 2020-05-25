using System;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;
using OsmSharp.Db.Tiled;
using RouteableTiles.API.Responses;
using RouteableTiles.IO.JsonLD.Semantics;
using RouteableTiles.IO.JsonLD.Tiles;

namespace RouteableTiles.API.Controllers
{
    /// <summary>
    /// Controller to return routeable tiles.
    /// </summary>
    /// <remarks>
    /// This implements the memento: http://www.mementoweb.org/guide/howto/
    ///
    /// The generic URI: {z}/{x}/{y} will redirect to the latest version URI.
    /// The version URI: {timestamp}/{z}/{x}/{y}
    /// 
    /// </remarks>
    [Route("/")]
    [ApiController]
    public class TilesController : ControllerBase
    {
        [HttpGet("{z}/{x}/{y}/")]
        public object GetJsonLD(uint z, uint x, uint y)
        {
            var historyDb = DatabaseInstance.Default;
            if (historyDb == null) return NotFound();
            
            // get the db closest to the accept-datetime header.
            DateTime? date = null;
            if (this.Request.Headers.TryGetValue("Accept-Datetime", out var value) &&
                DateTime.TryParseExact(value.ToString(), "ddd, dd MMM yyyy HH:mm:ss G\\MT", System.Globalization.CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal, out var utcDate))
            {
                date = utcDate.ToUniversalTime();
            }
            
            // get the db for the given date.
            var dbOn = historyDb.Latest;
            if (date != null) dbOn = historyDb.GetSmallestOn(date.Value);
            if (dbOn == null) return NotFound();
            
            // gets the db for the tile.
            var dbForTile = historyDb.GetDbForTile(dbOn, (x, y));
            if (dbForTile == null) return NotFound();
            date = dbForTile.EndTimestamp;

            var timestamp =
                $"{date.Value.Year:0000}{date.Value.Month:00}{date.Value.Day:00}-{date.Value.Hour:00}{date.Value.Minute:00}{date.Value.Second:00}";

            var routeValues = new RouteValueDictionary {["timestamp"] = timestamp, ["x"] = x, ["y"] = y, ["z"] = z};
            return new RedirectToActionResult(nameof(GetJsonLDVersion), "Tiles", routeValues);
        }
        
        [HttpGet("{timestamp}/{z}/{x}/{y}/")]
        public object GetJsonLDVersion(string timestamp, uint z, uint x, uint y)
        {
            // parse the timestamp and make sure it's UTC.
            if (!DateTime.TryParseExact(timestamp, "yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out var utcDate))
            {
                return NotFound();
            }
            utcDate = utcDate.ToUniversalTime();
            
            // get the db.
            var historyDb = DatabaseInstance.Default;
            if (historyDb == null) return NotFound();

            // gets the db on the date.
            var dbOn = historyDb.GetSmallestOn(utcDate);
            if (dbOn == null) return NotFound();
            if (z != dbOn.Zoom) return NotFound();
            
            // check if db has the tile, otherwise redirect.
            var dbForTile = historyDb.GetDbForTile(dbOn, (x, y));
            if (dbForTile != null && dbForTile.EndTimestamp != dbOn.EndTimestamp)
            {
                var date = dbForTile.EndTimestamp;
                
                timestamp =
                    $"{date.Year:0000}{date.Month:00}{date.Day:00}-{date.Hour:00}{date.Minute:00}{date.Second:00}";
                var routeValues = new RouteValueDictionary {["timestamp"] = timestamp, ["x"] = x, ["y"] = y, ["z"] = z};
                return new RedirectToActionResult(nameof(GetJsonLDVersion), "Tiles", routeValues);
            }
            
            // the db has the tile, return it.
            var data = dbOn.GetRouteableTile((x, y), (ts) => ts.IsRelevant(
                JsonLDTileResponseFormatter.MappingKeys ?? TagMapper.DefaultMappingKeys, 
                JsonLDTileResponseFormatter.Mapping ?? TagMapper.DefaultMappingConfigs));

            var baseUrl = this.Request.HttpContext.Request.BasePath();
            if (!baseUrl.EndsWith("/")) baseUrl += "/";
            baseUrl = $"{baseUrl}{timestamp}/{z}/{x}/{y}/";
            
            Response.Headers[HeaderNames.CacheControl] = "public,max-age=" + (int)TimeSpan.FromDays(1).TotalSeconds;
            Response.Headers["Memento-DateTime"] = utcDate.ToString("ddd, dd MMM yyyy HH:mm:ss G\\MT");
            Response.Headers["Link"] = $"<{baseUrl}>; rel=\"original\"";

            var tile = new Tile(x, y, z);
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
            var data = db.Latest.GetRouteableTile((x, y), (ts) => ts.IsRelevant(
                JsonLDTileResponseFormatter.MappingKeys ?? TagMapper.DefaultMappingKeys, 
                JsonLDTileResponseFormatter.Mapping ?? TagMapper.DefaultMappingConfigs));
            
            Response.Headers[HeaderNames.CacheControl] = "public,max-age=" + 60;

            // TODO: handle this properly, what do we do when application/xml was not requested?
            // force an xml response.
            Request.Headers[HeaderNames.Accept] = "application/xml";

            return new OsmTileResponse(data, tile);
        }
    }
}