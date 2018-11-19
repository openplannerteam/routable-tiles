using OsmSharp.Db.Tiled.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OsmSharp.Db.Tiled.Tests.Mocks
{
    public class MockFileSystem : IFileSystem
    {
        private readonly Dir _root;

        public MockFileSystem(string root)
        {
            _root = new Dir(root);
        }

        private string[] Get(string path)
        {
            var relativePath = path.Substring(_root.Name.Length,
                path.Length - _root.Name.Length);
            return relativePath.Split('\\', '/');
        }

        private Dir FindDir(string directory)
        {
            var dirs = this.Get(directory);
            var current = _root;
            for (var i = 0; i < dirs.Length; i++)
            {
                var name = dirs[i];

                Dir subDir;
                if (!current.SubDirs.TryGetValue(name, out subDir))
                {
                    return null;
                }
                current = subDir;
            }
            return current;
        }

        private File FindFile(string fullFileName)
        {
            var dir = FindDir(this.DirectoryForFile(fullFileName));

            if (dir == null)
            {
                return null;
            }
            var fileName = this.FileName(fullFileName);
            File file;
            if (dir.Files.TryGetValue(fileName, out file))
            {
                return file;
            }
            return null;
        }

        public string Combine(params string[] paths)
        {
            return Path.Combine(paths);
        }

        public void CreateDirectory(string directory)
        {
            var dirs = this.Get(directory);
            var current = _root;
            for (var i = 0; i < dirs.Length; i++)
            {
                var name = dirs[i];

                Dir subDir;
                if (!current.SubDirs.TryGetValue(name, out subDir))
                {
                    subDir = new Dir(name);
                    current.SubDirs[name] = subDir;
                }

                current = subDir;
            }
        }

        public void Delete(string file)
        {
            var directory = this.DirectoryForFile(file);
            file = this.FileName(file);

            var dir = this.FindDir(directory);
            if (dir == null)
            {
                throw new Exception("Directory not found.");
            }
            dir.Files.Remove(file);
        }

        public bool DirectoryExists(string directory)
        {
            return this.FindDir(directory) != null;
        }

        public string DirectoryForFile(string file)
        {
            return new FileInfo(file).Directory.FullName;
        }

        public string DirectoryName(string directory)
        {
            return Path.GetDirectoryName(directory);
        }

        public IEnumerable<string> EnumerateDirectories(string directory)
        {
            var dir = this.FindDir(directory);
            if (dir == null)
            {
                throw new Exception("Directory not found.");
            }
            return dir.SubDirs.Select(x => Path.Combine(directory, x.Value.Name));
        }

        public IEnumerable<string> EnumerateFiles(string directory, string mask = null)
        {
            var dir = this.FindDir(directory);
            if (dir == null)
            {
                throw new Exception("Directory not found.");
            }
            return dir.Files.Select(x => Path.Combine(directory, x.Value.Name));
        }

        public bool Exists(string file)
        {
            return this.FindFile(file) != null;
        }

        public string FileName(string file)
        {
            return Path.GetFileName(file);
        }

        public Stream Open(string file, FileMode mode)
        {
            if (mode != FileMode.Open &&
                mode != FileMode.Append)
            {
                if (!this.Exists(file))
                {
                    var dir = this.FindDir(this.DirectoryForFile(file));
                    var name = this.FileName(file);
                    dir.Files.Add(name,
                        new File()
                        {
                            Data = new byte[0],
                            Name = name
                        });
                }
            }
            return new MockStream(this.FindFile(file));
        }

        public Stream OpenRead(string location)
        {
            return this.Open(location, FileMode.Open);
        }

        public Stream OpenWrite(string location)
        {
            return this.Open(location, FileMode.Append);
        }

        public override string ToString()
        {
            return _root.ToString();
        }

        class Dir
        {
            public Dir(string name)
            {
                this.Name = name;
                this.SubDirs = new Dictionary<string, Dir>();
                this.Files = new Dictionary<string, File>();
            }

            public string Name { get; private set; }

            public Dictionary<string, Dir> SubDirs { get; private set; }

            public Dictionary<string, File> Files { get; private set; }

            public string ToString(string parent)
            {
                var stringBuilder = new StringBuilder();
                var dir = FileSystemFacade.FileSystem.Combine(parent, this.Name);
                stringBuilder.AppendLine(dir);
                foreach (var file in this.Files)
                {
                    stringBuilder.AppendLine(FileSystemFacade.FileSystem.Combine(dir, 
                        string.Format("{0}", file.Key)));
                }
                foreach (var d in this.SubDirs)
                {
                    stringBuilder.Append(d.Value.ToString(dir));
                }
                return stringBuilder.ToInvariantString();
            }

            public override string ToString()
            {
                return this.ToString(string.Empty);
            }
        }

        public class File
        {
            public string Name { get; set; }

            public byte[] Data { get; set; }
        }
    }
}
