using System;
using System.IO;
using System.Linq;
using Itinero;
using Itinero.Algorithms.Networks;
using Itinero.Algorithms.Search.Hilbert;
using Itinero.IO.Osm;
using Itinero.IO.Osm.Streams;
using Itinero.LocalGeo;
using Itinero.Logging;
using Itinero.Profiles;
using OsmSharp.Streams;
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
            //     "14",
            //     "tile-stats.csv"
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
            if (!uint.TryParse(args[2], out var zoom))
            {
                Log.Information("Cannot parse zoom-level, expected [0-20]: " + args[2]);
                return;
            }

            var tileStats = string.Empty;
            if (args.Length >= 4)
            {
                tileStats = args[3];
            }

            // load routerdb.
            RouterDb routerDb = null;
            var sourceFile = args[0];
            if (sourceFile.EndsWith(".osm.pbf"))
            {
                routerDb = new RouterDb();
                using (var stream = File.OpenRead(args[0]))
                {
                    var source = new PBFOsmStreamSource(stream);
                    var progress = new OsmSharp.Streams.Filters.OsmStreamFilterProgress();
                    progress.RegisterSource(source);

                    // make sure the routerdb can handle multiple edges.
                    routerDb.Network.GeometricGraph.Graph.MarkAsMulti();


                    var vehicles = new Vehicle[]
                    {
                        Itinero.Osm.Vehicles.Vehicle.Car,
                        Itinero.Osm.Vehicles.Vehicle.Bicycle,
                        Itinero.Osm.Vehicles.Vehicle.Pedestrian
                    };

                    // load the data.
                    var settings = new LoadSettings();
                    var target = new RouterDbStreamTarget(routerDb,
                        vehicles, false, processRestrictions: false, processors: settings.Processors,
                        simplifyEpsilonInMeter: settings.NetworkSimplificationEpsilon);
                    target.KeepNodeIds = settings.KeepNodeIds;
                    target.KeepWayIds = settings.KeepWayIds;
                    target.RegisterSource(progress);
                    target.Pull();

                    // optimize the network for routing.
                    routerDb.SplitLongEdges();
                    routerDb.ConvertToSimple();

                    // sort the network.
                    routerDb.Sort();

                    // optimize the network if requested.
                    if (settings.NetworkSimplificationEpsilon > 0)
                    {
                        routerDb.OptimizeNetwork(settings.NetworkSimplificationEpsilon);
                    }

                    Itinero.Logging.Logger.Log("Program", TraceEventType.Information, "Writing output routerdb...");
                    using (var output = File.Open("output.routerdb", FileMode.Create))
                    {
                        routerDb.Serialize(output);
                    }
                }
            }
            else if (sourceFile.EndsWith(".routerdb"))
            {
                Log.Information($"Loading {sourceFile}");
                var stream = File.OpenRead(sourceFile);
                routerDb = RouterDb.Deserialize(stream);
            }
            else
            {
                Log.Information("Cannot process source file, .osm.pbf or .routerdb expected: " + args[0]);
                return;
            }

            Log.Information("Data loaded, generating tiles...");

            // extract tiles.
            var location = routerDb.Network.GetVertex(0);
            float minLat = location.Latitude,
                minLon = location.Longitude,
                maxLat = location.Latitude,
                maxLon = location.Longitude;
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

            var box = new Box(minLat, minLon, maxLat, maxLon);
            //var tiles = ().GetTilesCovering(zoom);
            var tiles = (new Tile(0, 0, 0)).GetSubtilesAt(zoom).ToList();

            Log.Information($"Extracting {tiles.Count} tiles in [{box.MinLat},{box.MinLon},{box.MaxLat},{box.MaxLon}]...");

            // extract all tiles.
            using (var stream = new MemoryStream())
            {
                for (var t = 0; t < tiles.Count; t++)
                {
                    if (t % 10000 == 0)
                    {
                        Log.Information($"{(float)t / tiles.Count * 100:F4}%");
                    }

                    var tile = tiles[t];
                    if (!tile.Box.Overlaps(box))
                    {
                        continue;
                    }

                    stream.SetLength(0);
                    stream.Seek(0, SeekOrigin.Begin);

                    var streamWriter = new StreamWriter(stream);
                    var result = routerDb.WriteRoutingTile(streamWriter, tile, x => x);
                    if (result.success)
                    {
                        streamWriter.Flush();
                        if (!string.IsNullOrWhiteSpace(tileStats))
                        {
                            File.AppendAllLines(tileStats, new[] { $"{tile.Zoom},{tile.X},{tile.Y},{result.vertices},{result.edges}" });
                        }
                    }

                    if (stream.Length > 0)
                    {
                        var file = Path.Combine(path, tile.Zoom.ToInvariantString(),
                            tile.X.ToInvariantString(), tile.Y.ToInvariantString() + ".geojson");
                        var fileInfo = new FileInfo(file);
                        if (!fileInfo.Directory.Exists)
                        {
                            fileInfo.Directory.Create();
                        }

                        Log.Information("Extracted tile:" + fileInfo.FullName);
                        stream.Seek(0, SeekOrigin.Begin);
                        using (var fileStream = fileInfo.Open(FileMode.Create))
                        {
                            stream.CopyTo(fileStream);
                        }
                    }

                }
            }
        }
    }
}
