using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using OsmSharp;
using OsmSharp.Streams;
using OsmSharp.Streams.Collections;
using RouteableTiles.IO.JsonLD;
using Serilog;

namespace RouteableTiles.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
//#if DEBUG
            if (args == null || args.Length == 0)
            {
                args = new string[]
                {
                    @"/data/work/data/OSM/belgium-latest.osm.pbf",
                    @"/media/xivk/2T-SSD-EXT/db-germany/",
                    @"14"
                };
            }
//#endif
            
            // enable logging.
            OsmSharp.Logging.Logger.LogAction = (origin, level, message, parameters) =>
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

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File(Path.Combine("logs", "log-.txt"), rollingInterval: RollingInterval.Day)
                .CreateLogger();

//            try
//            {
                // validate arguments.
                if (args.Length < 3)
                {
                    Log.Fatal("Expected 3 arguments: inputfile outputdir zoom");
                    return;
                }
                if (!File.Exists(args[0]))
                {
                    Log.Fatal("Input file not found: {0}", args[0]);
                    return;
                }
                if (!Directory.Exists(args[1]))
                {
                    Log.Fatal("Cache directory doesn't exist: {0}", args[1]);
                    return;
                }
                if (!uint.TryParse(args[2], out var zoom))
                {
                    Log.Fatal("Can't parse zoom: {0}", args[2]);
                    return;
                }

                var ticks = DateTime.Now.Ticks;
                const bool compressed = true;
                if (!File.Exists(Path.Combine(args[1], "0", "0", "0.nodes.idx")))
                {
                    Log.Information("Building the tiled DB...");
                    var source = new OsmSharp.Streams.PBFOsmStreamSource(
                        File.OpenRead(args[0]));
                    var progress = new OsmSharp.Streams.Filters.OsmStreamFilterProgress();
                    progress.RegisterSource(source);
                    
                    // filter out routeable data only.
                    var nodeIndex = new OsmIdIndex();
                    while (progress.MoveNext(true, false, true))
                    {
                        if (!(progress.Current() is Way w) || w.Nodes == null) continue;
                        if (w.Tags == null || !w.Tags.ContainsKey("highway")) continue;
                
                        foreach (var n in w.Nodes)
                        {
                            nodeIndex.Add(n);
                        }
                    }

                    var filtered = new OsmEnumerableStreamSource(progress.Where(x =>
                    {
                        if (x is Node)
                        {
                            return nodeIndex.Contains(x.Id.Value);
                        }
                        else if (x is Way)
                        {
                            return x.Tags != null && x.Tags.ContainsKey("highway");
                        }
                        else
                        {
                            if (x.Tags == null) return false;
                            return x.Tags.ContainsKey("route") ||
                                   x.Tags.ContainsKey("restriction");
                        }
                    }));

                    // splitting tiles and writing indexes.
                    Build.Builder.Build(filtered, args[1], zoom, compressed);
                }
                else
                {
                    Log.Error("The tiled DB already exists, cannot overwrite, delete it first...");
                }
//            }
//            catch (Exception e)
//            {
//                Log.Fatal(e, "Unhandled exception.");
//                throw;
//            }
        }
    }
}