using OsmSharp.Db.Tiled.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace OsmSharp.Db.Tiled.Indexes
{
    public static class IndexExtensions
    {
        /// <summary>
        /// Writes the given index to a given tile async.
        /// </summary>
        public static void WriteAsync(this Index index, string filename)
        {
            ThreadPool.QueueUserWorkItem(state =>
            {
                var filename_state = state as string;
                var directory = FileSystemFacade.FileSystem.DirectoryForFile(filename_state);
                //var fileInfo = new FileInfo(filename_state);
                if (!FileSystemFacade.FileSystem.DirectoryExists(directory))
                //if (!fileInfo.Directory.Exists)
                {
                    //fileInfo.Directory.Create();
                    FileSystemFacade.FileSystem.CreateDirectory(directory);
                }
                using (var stream = FileSystemFacade.FileSystem.Open(filename_state, FileMode.Create))
                //using (var stream = File.Open(filename_state, FileMode.Create))
                {
                    index.Serialize(stream);
                }
            }, filename);
        }
    }
}