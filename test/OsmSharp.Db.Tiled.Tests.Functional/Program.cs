using System;
using System.IO;
using Serilog;
using System.Collections.Generic;
using OsmSharp.Db.Tiled.Ids;
using OsmSharp.Logging;

namespace OsmSharp.Db.Tiled.Tests.Functional
{
    class Program
    {
        static void Main(string[] args)
        {
            args = new string[]
            {
                @"/home/xivk/work/data/OSM/luxembourg-latest.osm.pbf",
                @"/home/xivk/work/anyways/data/tiled-osm-db/db",
                @"/home/xivk/work/anyways/data/tiled-osm-db/complete"
            };

            uint zoom = 14;
            
            OsmSharp.Logging.Logger.LogAction = (o, level, message, parameters) =>
            {
#if RELEASE
                if (level == "verbose")
                {
                    return;
                }
#endif
                if (level == TraceEventType.Verbose.ToString().ToLower())
                {
                    Log.Debug(string.Format("[{0}] {1} - {2}", o, level, message));
                }
                else if (level == TraceEventType.Information.ToString().ToLower())
                {
                    Log.Information(string.Format("[{0}] {1} - {2}", o, level, message));
                }
                else if (level == TraceEventType.Warning.ToString().ToLower())
                {
                    Log.Warning(string.Format("[{0}] {1} - {2}", o, level, message));
                }
                else if (level == TraceEventType.Critical.ToString().ToLower())
                {
                    Log.Fatal(string.Format("[{0}] {1} - {2}", o, level, message));
                }
                else if (level == TraceEventType.Error.ToString().ToLower())
                {
                    Log.Error(string.Format("[{0}] {1} - {2}", o, level, message));
                }
                else
                {
                    Log.Debug(string.Format("[{0}] {1} - {2}", o, level, message));
                }
            };

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.LiterateConsole()
                .CreateLogger();

            // build db.
            var source = new OsmSharp.Streams.PBFOsmStreamSource(
                File.OpenRead(args[0]));
            var progress = new OsmSharp.Streams.Filters.OsmStreamFilterProgress();
            progress.RegisterSource(source);

            // building database.
            var ticks = DateTime.Now.Ticks;
            Build.Builder.Build(progress, args[1], zoom);
            var span = new TimeSpan(DateTime.Now.Ticks - ticks);
            Console.WriteLine("Splitting took {0}s", span);

            // reading some data.
            Console.WriteLine("Loading database...");
            var db = new Database(args[1]);
            
            // write some complete tiles.
            if (!Directory.Exists(args[2]))
            {
                throw new Exception();
            }
            foreach (var baseTile in db.GetTiles())
            {
                Console.WriteLine("Base tile found: {0}", baseTile);

                var file = Path.Combine(args[2], baseTile.Zoom.ToInvariantString(), baseTile.X.ToInvariantString(),
                    baseTile.Y.ToInvariantString() + ".osm");
                var fileInfo = new FileInfo(file);
                if (fileInfo.Directory != null && !fileInfo.Directory.Exists)
                {
                    fileInfo.Directory.Create();
                }

                using (var stream = File.Open(file, FileMode.Create))
                {
                    var target = new OsmSharp.Streams.XmlOsmStreamTarget(stream);
                    target.Initialize();

                    db.GetCompleteTile(baseTile, target);

                    target.Flush();
                    target.Close();
                }
            }

            Console.WriteLine("Testing done!");
            Console.ReadLine();
        }
    }
}