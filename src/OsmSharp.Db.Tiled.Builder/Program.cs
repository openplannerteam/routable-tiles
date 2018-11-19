using System;
using System.IO;
using Serilog;

namespace OsmSharp.Db.Tiled.Builder
{
    class Program
    {
        static void Main(string[] args)
        {
            OsmSharp.Logging.Logger.LogAction = (o, level, message, parameters) =>
            {
                Console.WriteLine(string.Format("[{0}] {1} - {2}", o, level, message));
            };

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.LiterateConsole()
                .CreateLogger();

            // validate arguments.
            if (args == null || args.Length < 3)
            {
                Log.Fatal("Expected 3 arguments: inputfile outputpath zoom");
            }
            if (!File.Exists(args[0]))
            {
                Log.Fatal("Input file not found: {0}", args[0]);
            }
            if (!Directory.Exists(args[1]))
            {
                Log.Fatal("Output directory doesn't exist: {0}", args[1]);
            }
            uint zoom;
            if (!uint.TryParse(args[2], out zoom))
            {
                Log.Fatal("Can't parse zoom: {0}", args[2]);
            }

            var source = new OsmSharp.Streams.PBFOsmStreamSource(
                File.OpenRead(args[0]));
            var progress = new OsmSharp.Streams.Filters.OsmStreamFilterProgress();
            progress.RegisterSource(source);

            // building database.
            var ticks = DateTime.Now.Ticks;
            Build.Builder.Build(progress, args[1], zoom);
            var span = new TimeSpan(DateTime.Now.Ticks - ticks);
            Console.WriteLine("Splitting took {0}s", span);
        }
    }
}
