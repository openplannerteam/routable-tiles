// using System;
// using System.Globalization;
// using System.Linq;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.AspNetCore.Routing;
// using Microsoft.Net.Http.Headers;
// using OsmSharp.Db.Tiled;
// using RouteableTiles.API.Responses;
//
// namespace RouteableTiles.API.Controllers
// {
//     [ApiController]
//     public class ChangesController : ControllerBase
//     {
//         [HttpGet("changes")]
//         public object GetChanges()
//         {
//             var historyDb = DatabaseInstance.Default;
//             if (historyDb == null) return NotFound();
//             
//             // assume latest db, unless accept-datetime header.
//             var db = historyDb.Latest;
//             if (db == null) return NotFound();
//             
//             // get the db closest to the accept-datetime header.
//             if (this.Request.Headers.TryGetValue("Accept-Datetime", out var value) &&
//                 DateTime.TryParseExact(value.ToString(), "ddd, dd MMM yyyy HH:mm:ss G\\MT", System.Globalization.CultureInfo.InvariantCulture,
//                     DateTimeStyles.AssumeUniversal, out var utcDate))
//             {
//                 utcDate = utcDate.ToUniversalTime();
//
//                 db = historyDb.GetOn(utcDate);
//                 if (db == null) return NotFound();
//             }
//             
//             var date = db.EndTimestamp;
//
//             var timestamp =
//                 $"{date.Year:0000}{date.Month:00}{date.Day:00}-{date.Hour:00}{date.Minute:00}{date.Second:00}";
//
//             var routeValues = new RouteValueDictionary {["timestamp"] = timestamp};
//             return new RedirectToActionResult(nameof(GetChangesVersion), "Changes", routeValues);
//         }
//         
//         [HttpGet("changes/{timestamp}")]
//         public object GetChangesVersion(string timestamp)
//         {
//             if (!DateTime.TryParseExact(timestamp, "yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture,
//                 DateTimeStyles.AssumeUniversal, out var utcDate))
//             {
//                 return NotFound();
//             }
//
//             utcDate = utcDate.ToUniversalTime();
//             
//             var db = DatabaseInstance.Default;
//             if (db == null) return NotFound();
//
//             var instance = db.GetOn(utcDate);
//             if (instance == null) return NotFound();
//             var previousDb = instance.GetPrevious();
//             var previous = previousDb == null ? null :
//                 $"{previousDb.EndTimestamp.Year:0000}{previousDb.EndTimestamp.Month:00}{previousDb.EndTimestamp.Day:00}-{previousDb.EndTimestamp.Hour:00}{previousDb.EndTimestamp.Minute:00}{previousDb.EndTimestamp.Second:00}";
//             
//             var changes = new ChangeResponse()
//             {
//                 Tiles = instance.GetTiles(true).Select(x => (timestamp, x.x, x.y, instance.Zoom)),
//                 Previous = previous
//             };
//
//             var baseUrl = this.Request.HttpContext.Request.BasePath();
//             if (!baseUrl.EndsWith("/")) baseUrl += "/";
//             baseUrl = $"{baseUrl}changes/";
//             
//             Response.Headers[HeaderNames.CacheControl] = "public,max-age=" + (int)TimeSpan.FromDays(1).TotalSeconds;
//             Response.Headers["Memento-DateTime"] = utcDate.ToString("ddd, dd MMM yyyy HH:mm:ss G\\MT");
//             Response.Headers["Link"] = $"<{baseUrl}>; rel=\"original\"";
//
//             return changes;
//         }
//     }
// }