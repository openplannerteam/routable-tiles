using NUnit.Framework;
using OsmSharp.Db.Tiled.IO;
using OsmSharp.Streams;
using System;

namespace OsmSharp.Db.Tiled.Tests.Build
{
    /// <summary>
    /// Contains builder tests.
    /// </summary>
    [TestFixture]
    public class BuilderTests
    {
        /// <summary>
        /// Tests building a database.
        /// </summary>
        [Test]
        public void TestBuildNode()
        {
            FileSystemFacade.FileSystem = new Mocks.MockFileSystem(@"C:\");
            FileSystemFacade.FileSystem.CreateDirectory(@"C:\data");

            // build the database.
            var osmGeos = new OsmGeo[]
            {
                new Node()
                {
                    Id = 0,
                    Latitude = 50,
                    Longitude = 4,
                    ChangeSetId = 1,
                    UserId = 1,
                    UserName = "Ben",
                    Visible = true,
                    TimeStamp = DateTime.Now,
                    Version = 1
                }
            };
            OsmSharp.Db.Tiled.Build.Builder.Build(
               new OsmEnumerableStreamSource(osmGeos), @"C:\data");

            var s = FileSystemFacade.FileSystem.ToString();

            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data"));

            // check if zoom-level dirs exist.
            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\0"));
            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\2"));
            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\4"));
            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\6"));
            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\8"));
            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\10"));
            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\12"));
            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\14"));

            // check per level for the proper files.
            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\0\0"));
            Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\0\0\0.nodes.idx"));
            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\2\2"));
            Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\2\2\1.nodes.idx"));
            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\4\8"));
            Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\4\8\5.nodes.idx"));
            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\6\32"));
            Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\6\32\21.nodes.idx"));
            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\8\130"));
            Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\8\130\86.nodes.idx"));
            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\10\523"));
            Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\10\523\347.nodes.idx"));
            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\12\2093"));
            Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\12\2093\1389.nodes.idx"));
            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\14\8374"));
            Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\14\8374\5556.nodes.osm.bin"));
        }

        /// <summary>
        /// Tests building a database.
        /// </summary>
        [Test]
        public void TestBuildNodesAndWay()
        {
            FileSystemFacade.FileSystem = new Mocks.MockFileSystem(@"C:\");
            FileSystemFacade.FileSystem.CreateDirectory(@"C:\data");

            // build the database.
            var osmGeos = new OsmGeo[]
            {
                new Node()
                {
                    Id = 0,
                    Latitude = 50,
                    Longitude = 4,
                    ChangeSetId = 1,
                    UserId = 1,
                    UserName = "Ben",
                    Visible = true,
                    TimeStamp = DateTime.Now,
                    Version = 1
                },
                new Node()
                {
                    Id = 1,
                    Latitude = 50,
                    Longitude = 4,
                    ChangeSetId = 1,
                    UserId = 1,
                    UserName = "Ben",
                    Visible = true,
                    TimeStamp = DateTime.Now,
                    Version = 1
                },
                new Way()
                {
                    Id = 0,
                    ChangeSetId = 1,
                    Nodes = new long[]
                    {
                        0, 1
                    },
                    Tags = null,
                    TimeStamp = DateTime.Now,
                    UserId = 1,
                    UserName = "Ben",
                    Version = 1,
                    Visible = true
                }
            };
            OsmSharp.Db.Tiled.Build.Builder.Build(
               new OsmEnumerableStreamSource(osmGeos), @"C:\data");

            var s = FileSystemFacade.FileSystem.ToString();

            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data"));

            // check if zoom-level dirs exist.
            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\0"));
            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\2"));
            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\4"));
            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\6"));
            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\8"));
            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\10"));
            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\12"));
            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\14"));

            // check per level for the proper files.
            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\0\0"));
            Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\0\0\0.nodes.idx"));
            Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\0\0\0.ways.idx"));
            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\2\2"));
            Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\2\2\1.nodes.idx"));
            Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\2\2\1.ways.idx"));
            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\4\8"));
            Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\4\8\5.nodes.idx"));
            Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\4\8\5.ways.idx"));
            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\6\32"));
            Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\6\32\21.nodes.idx"));
            Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\6\32\21.ways.idx"));
            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\8\130"));
            Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\8\130\86.nodes.idx"));
            Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\8\130\86.ways.idx"));
            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\10\523"));
            Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\10\523\347.nodes.idx"));
            Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\10\523\347.ways.idx"));
            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\12\2093"));
            Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\12\2093\1389.nodes.idx"));
            Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\12\2093\1389.ways.idx"));
            Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\14\8374"));
            Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\14\8374\5556.nodes.osm.bin"));
            Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\14\8374\5556.ways.osm.bin"));
        }

        ///// <summary>
        ///// Tests building a database.
        ///// </summary>
        //[Fact]
        //public void TestBuildNodesAndWayAndRelation()
        //{
        //    FileSystemFacade.FileSystem = new Mocks.MockFileSystem(@"C:\");
        //    FileSystemFacade.FileSystem.CreateDirectory(@"C:\data");

        //    // build the database.
        //    var osmGeos = new OsmGeo[]
        //    {
        //        new Node()
        //        {
        //            Id = 0,
        //            Latitude = 50,
        //            Longitude = 4,
        //            ChangeSetId = 1,
        //            UserId = 1,
        //            UserName = "Ben",
        //            Visible = true,
        //            TimeStamp = DateTime.Now,
        //            Version = 1
        //        },
        //        new Node()
        //        {
        //            Id = 1,
        //            Latitude = 50,
        //            Longitude = 4,
        //            ChangeSetId = 1,
        //            UserId = 1,
        //            UserName = "Ben",
        //            Visible = true,
        //            TimeStamp = DateTime.Now,
        //            Version = 1
        //        },
        //        new Way()
        //        {
        //            Id = 0,
        //            ChangeSetId = 1,
        //            Nodes = new long[]
        //            {
        //                0, 1
        //            },
        //            Tags = null,
        //            TimeStamp = DateTime.Now,
        //            UserId = 1,
        //            UserName = "Ben",
        //            Version = 1,
        //            Visible = true
        //        },
        //        new Relation()
        //        {
        //            Id = 0,
        //            ChangeSetId = 1,
        //            Members = new RelationMember[]
        //            {
        //                new RelationMember()
        //                {
        //                    Id = 0,
        //                    Role = "",
        //                    Type = OsmGeoType.Node
        //                },
        //                new RelationMember()
        //                {
        //                    Id = 1,
        //                    Role = "",
        //                    Type = OsmGeoType.Node
        //                },
        //                new RelationMember()
        //                {
        //                    Id = 0,
        //                    Role = "",
        //                    Type = OsmGeoType.Way
        //                }
        //            },
        //            Tags = null,
        //            TimeStamp = DateTime.Now,
        //            UserId = 1,
        //            UserName = "Ben",
        //            Version = 1,
        //            Visible = true
        //        }
        //    };
        //    OsmSharp.Db.Tiled.Build.Builder.Build(
        //       new OsmEnumerableStreamSource(osmGeos), @"C:\data");

        //    var s = FileSystemFacade.FileSystem.ToString();

        //    Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data"));

        //    // check if zoom-level dirs exist.
        //    Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\0"));
        //    Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\2"));
        //    Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\4"));
        //    Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\6"));
        //    Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\8"));
        //    Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\10"));
        //    Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\12"));
        //    Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\14"));

        //    // check per level for the proper files.
        //    Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\0\0"));
        //    Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\0\0\0.nodes.idx"));
        //    Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\0\0\0.ways.idx"));
        //    Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\2\2"));
        //    Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\2\2\1.nodes.idx"));
        //    Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\2\2\1.ways.idx"));
        //    Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\4\8"));
        //    Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\4\8\5.nodes.idx"));
        //    Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\4\8\5.ways.idx"));
        //    Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\6\32"));
        //    Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\6\32\21.nodes.idx"));
        //    Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\6\32\21.ways.idx"));
        //    Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\8\130"));
        //    Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\8\130\86.nodes.idx"));
        //    Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\8\130\86.ways.idx"));
        //    Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\10\523"));
        //    Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\10\523\347.nodes.idx"));
        //    Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\10\523\347.ways.idx"));
        //    Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\12\2093"));
        //    Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\12\2093\1389.nodes.idx"));
        //    Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\12\2093\1389.ways.idx"));
        //    Assert.True(FileSystemFacade.FileSystem.DirectoryExists(@"C:\data\14\8374"));
        //    Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\14\8374\5556.nodes.osm.bin"));
        //    Assert.True(FileSystemFacade.FileSystem.Exists(@"C:\data\14\8374\5556.ways.osm.bin"));
        //}
    }
}
