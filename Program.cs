﻿using System;
using System.IO;
using Itinero;
using Itinero.IO.Osm;
using Itinero.LocalGeo;
using routable_tiles.Tiles;
using Serilog;

namespace routable_tiles
{
    class Program
    {
        static void Main(string[] args)
        {
            // setup logging.
            Log.Logger = new LoggerConfiguration()
                .WriteTo.LiterateConsole()
                .CreateLogger();
            OsmSharp.Logging.Logger.LogAction = (o, level, message, parameters) =>
            {
                Log.Information(string.Format("[{0}] {1} - {2}", o, level, message));
            };
            Itinero.Logging.Logger.LogAction = (o, level, message, parameters) =>
            {
                Log.Information(string.Format("[{0}] {1} - {2}", o, level, message));
            };

            // parse command-line arguments.
            var expectedUsage = @"Expected Arguments: " + Environment.NewLine + 
                "  [sourcefile] [outputfolder] [zoom-level]" + Environment.NewLine + 
                "Example: " + Environment.NewLine +
                "  /path/to/osm-file.osm.pbf /output/path 14";
    
            // args = new string[]
            // {
            //     "/home/xivk/data/wechel.osm.pbf",
            //     "/home/xivk/work/itinero/routable-tiles/output",
            //     "14"
            // };
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
            var path = args[1];
            uint zoom;
            if (!uint.TryParse(args[2], out zoom))
            {
                Log.Information("Cannot parse zoom-level, expected [0-20]: " + args[2]);
                return;
            }

            // load routerdb.
            var routerDb = new RouterDb();
            using (var stream = File.OpenRead(args[0]))
            {
                routerDb.LoadOsmData(stream, new LoadSettings()
                    {
                        KeepNodeIds = true
                    }, 
                    Itinero.Osm.Vehicles.Vehicle.Car, 
                    Itinero.Osm.Vehicles.Vehicle.Bicycle,
                    Itinero.Osm.Vehicles.Vehicle.Pedestrian);
            }

            // extract tiles.
            var location = routerDb.Network.GetVertex(0);
            float minLat = location.Latitude, minLon = location.Longitude, 
                maxLat = location.Latitude, maxLon = location.Longitude;
            for (uint v = 1; v < routerDb.Network.VertexCount; v++)
            {
                location = routerDb.Network.GetVertex(v);

                if (location.Latitude < minLat)
                {
                    minLat = location.Latitude;
                }
                if (location.Latitude > maxLat)
                {
                    maxLat = location.Latitude;
                }
                if (location.Longitude < minLon)
                {
                    minLon = location.Longitude;
                }
                if (location.Longitude > maxLon)
                {
                    maxLon = location.Longitude;
                }
            }
            var tiles = (new Box(minLat, minLon, maxLat, maxLon)).GetTilesCovering(zoom);

            // extract all tiles.
            foreach (var tile in tiles)
            {
                using (var stream = new StringWriter())
                {
                    if (routerDb.WriteRoutingTile(stream, tile, x => x))
                    {
                        var file = Path.Combine(path, tile.Zoom.ToInvariantString(), tile.X.ToInvariantString(),
                                    tile.Y.ToInvariantString() + ".geojson");
                        var fileInfo = new FileInfo(file);
                        if (!fileInfo.Directory.Exists)
                        {
                            fileInfo.Directory.Create();
                        }

                        File.WriteAllText(file, stream.ToString());

                        Log.Information("Extracted tile:" + fileInfo.FullName);
                    }
                }
            }
        }
    }
}
