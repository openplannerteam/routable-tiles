using System;
using System.Collections.Generic;
using System.IO;
using OsmSharp;
using OsmSharp.IO.Binary;
using OsmSharp.Streams;
using RoutableTiles.Build.Indexes;
using RoutableTiles.IO;
using RoutableTiles.Tiles;

namespace RoutableTiles.Build
{
    /// <summary>
    /// The relation processor.
    /// </summary>
    internal static class RelationProcessor
    {
        /// <summary>
        /// Processes the relations in the given stream. Assumed the current stream position already contains a relation.
        /// </summary>
        /// <param name="source">The source stream.</param>
        /// <param name="path">The based path of the db.</param>
        /// <param name="maxZoom">The maximum zoom.</param>
        /// <param name="tile">The tile being split.</param>
        /// <param name="nodeIndex">The node index.</param>
        /// <param name="wayIndex">The way index.</param>
        /// <returns>The indexed node id's with a masked zoom.</returns>
        public static Index Process(OsmStreamSource source, string path, uint maxZoom, Tile tile,
            Index nodeIndex, Index wayIndex)
        {
            // split relations.
            var subtiles = new Dictionary<ulong, Stream>();
            foreach (var subTile in tile.GetSubtilesAt(tile.Zoom + 2))
            {
                subtiles.Add(subTile.LocalId, null);
            }

            // build the relations index.
            var relationIndex = new Index();
            do
            {
                var current = source.Current();
                if (current.Type != OsmGeoType.Relation)
                {
                    break;
                }

                // calculate tile.
                var r = (current as Relation);
                if (r?.Members == null)
                {
                    continue;
                }

                int? mask = 0;
                foreach (var member in r.Members)
                {
                    switch (member.Type)
                    {
                        case OsmGeoType.Node:
                            if (nodeIndex.TryGetMask(member.Id, out var nodeMask))
                            {
                                mask |= nodeMask;
                            }
                            break;
                        case OsmGeoType.Way:
                            if (wayIndex.TryGetMask(member.Id, out var wayMask))
                            {
                                mask |= wayMask;
                            }
                            break;
                        case OsmGeoType.Relation:
                            mask = null;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    if (mask == null)
                    { // fail fast.
                        break;
                    }
                }

                if (mask == null)
                { // could not determine mask.
                    continue;
                }

                // add way to output(s).
                foreach(var wayTile in tile.SubTilesForMask2(mask.Value))
                {
                    // is tile a subtile.
                    if (!subtiles.TryGetValue(wayTile.LocalId, out var stream))
                    {
                        continue;
                    }

                    // initialize stream if needed.
                    if (stream == null)
                    {
                        var file = FileSystemFacade.FileSystem.Combine(path, wayTile.Zoom.ToInvariantString(), wayTile.X.ToInvariantString(),
                            wayTile.Y.ToInvariantString() + ".relations.osm.bin");
                        var fileDirectory = FileSystemFacade.FileSystem.DirectoryForFile(file);
                        if (!FileSystemFacade.FileSystem.DirectoryExists(fileDirectory))
                        {
                            FileSystemFacade.FileSystem.CreateDirectory(fileDirectory);
                        }
                        stream = FileSystemFacade.FileSystem.Open(file, FileMode.Create);

                        subtiles[wayTile.LocalId] = stream;
                    }

                    // write way.
                    stream.Append(r);
                }
                
                // add way to index.
                relationIndex.Add(r.Id.Value, mask.Value);
            } while (source.MoveNext());

            // flush/dispose all subtile streams.
            foreach (var subtile in subtiles)
            {
                if (subtile.Value == null) continue;
                subtile.Value.Flush();
                subtile.Value.Dispose();
            }

            return relationIndex;
        }
    }
}