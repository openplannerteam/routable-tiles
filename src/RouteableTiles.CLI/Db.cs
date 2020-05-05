using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OsmSharp.Changesets;
using OsmSharp.Db.Tiled;
using OsmSharp.Db.Tiled.OsmTiled;
using OsmSharp.Replication;
using OsmSharp.Streams;
using Serilog;

namespace RouteableTiles.CLI
{
    internal static class Db
    {
        public static async Task<(OsmTiledDbBase db, IEnumerable<(uint x, uint y)> updatedTiles)> BuildOrUpdate(
            string osmPbfFile, string dbPath,
            bool update = false)
        {
            // try loading the db, if it doesn't exist build it.
            var ticks = DateTime.Now.Ticks;
            if (!OsmTiledHistoryDb.TryLoad(dbPath, out var db))
            {
                Log.Information("The DB doesn't exist yet, building...");

                var source = new PBFOsmStreamSource(
                    File.OpenRead(osmPbfFile));
                var progress = new OsmSharp.Streams.Filters.OsmStreamFilterProgress();
                progress.RegisterSource(source);

                // splitting tiles and writing indexes.
                db = OsmTiledHistoryDb.Create(dbPath, progress);
                Log.Information("DB built successfully.");
                Log.Information($"Took {new TimeSpan(DateTime.Now.Ticks - ticks).TotalSeconds}s");
                return (db.Latest, db.Latest.GetTiles());
            }

            if (db == null) throw new Exception("Db loading failed!");
            if (!update) return (db.Latest, db.Latest.GetTiles());
            Log.Information("DB loaded successfully.");

            ticks = DateTime.Now.Ticks;
            // play catchup if the database is behind more than one hour.
            // try downloading the latest hour.
            var changeSets = new List<OsmChange>();
            ReplicationState? latestStatus = null;
            if ((DateTime.Now.ToUniversalTime() - db.Latest.EndTimestamp).TotalHours > 1)
            {
                // the data is pretty old, update per hour.
                var hourEnumerator = await ReplicationConfig.Hourly.GetDiffEnumerator(db.Latest);
                if (hourEnumerator != null)
                {
                    if (await hourEnumerator.MoveNext())
                    {
                        Log.Verbose($"Downloading diff: {hourEnumerator.State}");
                        var diff = await hourEnumerator.Diff();
                        if (diff != null)
                        {
                            latestStatus = hourEnumerator.State;
                            changeSets.Add(diff);
                        }
                    }
                }
            }
            else
            {
                // the data is pretty recent, start doing minutes, do as much as available.
                var minuteEnumerator = await ReplicationConfig.Minutely.GetDiffEnumerator(db.Latest);
                if (minuteEnumerator != null)
                {
                    while (await minuteEnumerator.MoveNext())
                    {
                        Log.Verbose($"Downloading diff: {minuteEnumerator.State}");
                        var diff = await minuteEnumerator.Diff();
                        if (diff == null) continue;

                        latestStatus = minuteEnumerator.State;
                        changeSets.Add(diff);
                    }
                }
            }

            // apply diffs.
            if (latestStatus == null)
            {
                Log.Information("No more changes, db is up to date.");
                return (db.Latest, Enumerable.Empty<(uint x, uint y)>());
            }

            // squash changes.
            Log.Verbose($"Squashing changes...");
            var changeSet = changeSets.Squash();

            // build meta data.
            var metaData = new List<(string key, string value)>
            {
                ("period", latestStatus.Config.Period.ToString()),
                ("sequence_number", latestStatus.SequenceNumber.ToString())
            };

            // apply diff.
            Log.Information($"Applying changes...");
            db.ApplyDiff(changeSet, latestStatus.EndTimestamp, metaData);
            Log.Information($"Took {new TimeSpan(DateTime.Now.Ticks - ticks).TotalSeconds}s");

            return (db.Latest, db.Latest.GetTiles(true));
        }
    }
}