using System.Collections.Generic;
using System.IO;

namespace OsmSharp.Db.Tiled.IO
{
    /// <summary>
    /// An abstract file system.
    /// </summary>
    public interface IFileSystem
    {
        /// <summary>
        /// Returns true if the given file exists.
        /// </summary>
        bool Exists(string file);

        /// <summary>
        /// Deletes the given file.
        /// </summary>
        /// <param name="file"></param>
        void Delete(string file);

        /// <summary>
        /// Returns the filename.
        /// </summary>
        string FileName(string file);

        /// <summary>
        /// Gets the directory the given file is in.
        /// </summary>
        string DirectoryForFile(string file);

        /// <summary>
        /// Returns true if the given directory exists.
        /// </summary>
        bool DirectoryExists(string directory);

        /// <summary>
        /// Returns the directory name.
        /// </summary>
        string DirectoryName(string directory);

        /// <summary>
        /// Creates the given directory.
        /// </summary>
        void CreateDirectory(string directory);

        /// <summary>
        /// Enumerates directories.
        /// </summary>
        IEnumerable<string> EnumerateDirectories(string directory);

        /// <summary>
        /// Enumerates files.
        /// </summary>
        IEnumerable<string> EnumerateFiles(string directory, string mask = null);

        /// <summary>
        /// Opens the given file for read-access.
        /// </summary>
        Stream OpenRead(string location);

        /// <summary>
        /// Opens the given file for write-access.
        /// </summary>
        Stream OpenWrite(string location);

        Stream Open(string file, FileMode mode);

        /// <summary>
        /// Combines an array of strings into a path.
        /// </summary>
        string Combine(params string[] paths);
    }
}
