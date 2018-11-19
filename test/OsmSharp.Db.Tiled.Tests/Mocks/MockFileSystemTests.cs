using NUnit.Framework;
using OsmSharp.Db.Tiled.IO;
using System;
using System.Collections.Generic;
using System.Text;

namespace OsmSharp.Db.Tiled.Tests.Mocks
{
    /// <summary>
    /// Contains tests for the mock file system.
    /// </summary>
    [TestFixture]
    public class MockFileSystemTests
    {
        [Test]
        public void Test()
        {
            FileSystemFacade.FileSystem = new Mocks.MockFileSystem(@"C:\");

            FileSystemFacade.FileSystem.CreateDirectory(@"C:\data");

            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data"));
            Assert.False(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data1"));
        }
    }
}
