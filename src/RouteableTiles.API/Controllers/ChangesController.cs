using System;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;
using OsmSharp.Db.Tiled;
using RouteableTiles.API.Responses;

namespace RouteableTiles.API.Controllers
{
    [ApiController]
    public class ChangesController : ControllerBase
    {
        [HttpGet("changes")]
        public object GetChanges()
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
            date = dbOn.EndTimestamp;

            var timestamp =
                $"{date.Value.Year:0000}{date.Value.Month:00}{date.Value.Day:00}-{date.Value.Hour:00}{date.Value.Minute:00}{date.Value.Second:00}";

            var routeValues = new RouteValueDictionary {["timestamp"] = timestamp};
            return new RedirectToActionResult(nameof(GetChangesVersion), "Changes", routeValues);
        }
        
        [HttpGet("changes/{timestamp}")]
        public object GetChangesVersion(string timestamp)
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
            
            // check if db matches exactly the timestamp, otherwise redirect.
            if (dbOn.EndTimestamp != utcDate)
            {
                var date = dbOn.EndTimestamp;
                
                timestamp =
                    $"{date.Year:0000}{date.Month:00}{date.Day:00}-{date.Hour:00}{date.Minute:00}{date.Second:00}";
                var routeValues = new RouteValueDictionary {["timestamp"] = timestamp};
                return new RedirectToActionResult(nameof(GetChangesVersion), "Changes", routeValues);
            }
            
            // get the previous and next.
            var previousDb = historyDb.GetSmallestPrevious(dbOn);
            var previousDate = previousDb?.EndTimestamp;
            var previous = previousDate == null ? string.Empty :
                $"{previousDate.Value.Year:0000}{previousDate.Value.Month:00}{previousDate.Value.Day:00}-{previousDate.Value.Hour:00}{previousDate.Value.Minute:00}{previousDate.Value.Second:00}";
            var nextDb = historyDb.GetSmallestNext(dbOn);
            var nextDate = nextDb?.EndTimestamp;
            var next = nextDate == null ? string.Empty :
                $"{nextDate.Value.Year:0000}{nextDate.Value.Month:00}{nextDate.Value.Day:00}-{nextDate.Value.Hour:00}{nextDate.Value.Minute:00}{nextDate.Value.Second:00}";
            
            var changes = new ChangeResponse()
            {
                Tiles = dbOn.GetTiles(true).Select(x => (timestamp, x.x, x.y, dbOn.Zoom)),
                Previous = previous,
                Next = next
            };

            var baseUrl = this.Request.HttpContext.Request.BasePath();
            if (!baseUrl.EndsWith("/")) baseUrl += "/";
            baseUrl = $"{baseUrl}changes/";
            
            Response.Headers[HeaderNames.CacheControl] = "public,max-age=" + (int)TimeSpan.FromDays(1).TotalSeconds;
            Response.Headers["Memento-DateTime"] = utcDate.ToString("ddd, dd MMM yyyy HH:mm:ss G\\MT");
            Response.Headers["Link"] = $"<{baseUrl}>; rel=\"original\"";

            return changes;
        }
    }
}