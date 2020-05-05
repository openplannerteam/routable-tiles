using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OsmSharp.Logging;
using RouteableTiles.IO.JsonLD;
using RouteableTiles.IO.JsonLD.Semantics;
using RouteableTiles.IO.JsonLD.Tiles;
using Serilog;

namespace RouteableTiles.CLI    
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            // enable logging.
            Logger.LogAction = (origin, level, message, parameters) =>
            {
                var formattedMessage = $"{origin} - {message}";
                switch (level)
                {
                    case "critical":
                        Log.Fatal(formattedMessage);
                        break;
                    case "error":
                        Log.Error(formattedMessage);
                        break;
                    case "warning":
                        Log.Warning(formattedMessage);
                        break;
                    case "verbose":
                        Log.Verbose(formattedMessage);
                        break;
                    case "information":
                        Log.Information(formattedMessage);
                        break;
                    default:
                        Log.Debug(formattedMessage);
                        break;
                }
            };
            OsmSharp.Db.Tiled.Logging.Log.LogAction = (type, message) =>
            {
                switch (type)
                {
                    case OsmSharp.Db.Tiled.Logging.TraceEventType.Critical:
                        Log.Fatal(message);
                        break;
                    case OsmSharp.Db.Tiled.Logging.TraceEventType.Error:
                        Log.Error(message);
                        break;
                    case OsmSharp.Db.Tiled.Logging.TraceEventType.Warning:
                        Log.Warning(message);
                        break;
                    case OsmSharp.Db.Tiled.Logging.TraceEventType.Verbose:
                        Log.Verbose(message);
                        break;
                    case OsmSharp.Db.Tiled.Logging.TraceEventType.Information:
                        Log.Information(message);
                        break;
                    default:
                        Log.Debug(message);
                        break;
                }
            };

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .WriteTo.File(Path.Combine("logs", "log-.txt"), rollingInterval: RollingInterval.Day)
                .CreateLogger();

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();

            var planetFile = config["planet"];
            var dbPath = config["db"];
            var tilesPath = config["tiles"];
            var baseUrl = config["url"];

            var lockFile = new FileInfo(Path.Combine(dbPath, "replication.lock"));
            // if (LockHelper.IsLocked(lockFile.FullName))
            // {
            //     return;
            // }

            try
            {
                LockHelper.WriteLock(lockFile.FullName);

                // build or update the database.
                var (db, modifiedTiles) = await Db.BuildOrUpdate(planetFile, dbPath, true);
                
                // build tiles.
                foreach (var tile in modifiedTiles)
                {
                    Log.Verbose($"Writing tile {tile}...");
                    var data = db.GetRouteableTile(tile, (ts) => 
                        ts.IsRelevant(TagMapper.DefaultMappingKeys, TagMapper.DefaultMappingConfigs));
                    
                    var tilePath = Path.Combine(tilesPath, db.Zoom.ToString());
                    if (!Directory.Exists(tilePath)) Directory.CreateDirectory(tilePath);

                    tilePath = Path.Combine(tilePath, tile.x.ToString());
                    if (!Directory.Exists(tilePath)) Directory.CreateDirectory(tilePath);

                    tilePath = Path.Combine(tilePath, $"{tile.y}.json");

                    await using var stream = File.Open(tilePath, FileMode.Create);
                    await using var writer = new StreamWriter(stream);
                    await data.WriteTo(writer, new Tile(tile.x, tile.y, db.Zoom), baseUrl, TagMapper.DefaultMappingConfigs);
                }
            }
            catch (Exception e)
            {
                Log.Fatal(e, "Unhandled exception during processing.");
            }
            finally
            {
                File.Delete(lockFile.FullName);
            }
        }
    }
}