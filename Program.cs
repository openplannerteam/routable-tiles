using System;
using System.IO;
using Serilog;

namespace routable_tiles
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.LiterateConsole()
                .CreateLogger();

            var expectedUsage = @"Expected Arguments: " + Environment.NewLine + 
                "  [sourcefile] [outputfolder] [zoom-level]" + Environment.NewLine + 
                "Example: " + Environment.NewLine +
                "  /path/to/osm-file.osm.pbf /output/path 14";
    
            if (args.Length < 3) 
            {
                Log.Information("Expected at least three arguments." + Environment.NewLine + expectedUsage);
                return;
            }
            if (!File.Exists(args[0]))
            {
                Log.Information("Source file not found: " + args[0]);
                return;
            }
            if (!Directory.Exists(args[1]))
            {
                Log.Information("Target folder not found: " + args[1]);
                return;
            }
            uint zoom;
            if (!uint.TryParse(args[2], out zoom))
            {
                Log.Information("Cannot parse zoom-level, expected [0-20]: " + args[2]);
                return;
            }
        }
    }
}
