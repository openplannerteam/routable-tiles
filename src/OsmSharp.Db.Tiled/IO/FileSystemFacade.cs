using System;
using System.Collections.Generic;
using System.Text;

namespace OsmSharp.Db.Tiled.IO
{
    /// <summary>
    /// Defines a facade for the file system.
    /// </summary>
    public static class FileSystemFacade
    {
        /// <summary>
        /// The default file system.
        /// </summary>
        public static IFileSystem FileSystem = new DefaultFileSystem();
    }
}