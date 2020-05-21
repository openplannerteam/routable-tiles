using System;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;
using RouteableTiles.API.Responses;
using RouteableTiles.IO.JsonLD.Semantics;
using RouteableTiles.IO.JsonLD.Tiles;
using Serilog;

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
            var db = DatabaseInstance.Default;
            
            if (db == null) return NotFound();
            
            var latest = db.Latest;
            if (latest == null) return NotFound();
            if (z != latest.Zoom) return NotFound();
            var date = latest.EndTimestamp;
            
            if (this.Request.Headers.TryGetValue("Accept-Datetime", out var value) &&
                DateTime.TryParseExact(value.ToString(), "ddd, dd MMM yyyy HH:mm:ss G\\MT", System.Globalization.CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal, out var utcDate))
            {
                utcDate = utcDate.ToUniversalTime();

                var dbAtDate = db.GetOn(utcDate);
                if (dbAtDate == null) return NotFound();
                if (z != dbAtDate.Zoom) return NotFound();
                date = dbAtDate.EndTimestamp;
            }
            
            var timestamp =
                $"{date.Year:0000}{date.Month:00}{date.Day:00}-{date.Hour:00}{date.Minute:00}{date.Second:00}";

            var routeValues = new RouteValueDictionary {["timestamp"] = timestamp, ["x"] = x, ["y"] = y, ["z"] = z};
            return new RedirectToActionResult(nameof(GetJsonLDVersion), "Tiles", routeValues);
        }
        
        [HttpGet("{timestamp}/{z}/{x}/{y}/")]
        public object GetJsonLDVersion(string timestamp, uint z, uint x, uint y)
        {
            if (!DateTime.TryParseExact(timestamp, "yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out var utcDate))
            {
                return NotFound();
            }

            utcDate = utcDate.ToUniversalTime();
            
            var db = DatabaseInstance.Default;
            if (db == null) return NotFound();

            var instance = db.GetOn(utcDate);
            if (instance == null) return NotFound();
            if (z != instance.Zoom) return NotFound();
            
            var data = instance.GetRouteableTile((x, y), (ts) => ts.IsRelevant(
                JsonLDTileResponseFormatter.MappingKeys ?? TagMapper.DefaultMappingKeys, 
                JsonLDTileResponseFormatter.Mapping ?? TagMapper.DefaultMappingConfigs));

            var baseUrl = this.Request.HttpContext.Request.BasePath();
            if (!baseUrl.EndsWith("/")) baseUrl = baseUrl + "/";
            baseUrl = $"{baseUrl}{z}/{x}/{y}/";
            
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