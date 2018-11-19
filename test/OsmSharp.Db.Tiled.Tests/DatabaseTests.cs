using NUnit.Framework;
using OsmSharp.Db.Tiled.Ids;
using OsmSharp.Db.Tiled.IO;
using System;

namespace OsmSharp.Db.Tiled.Tests
{
    /// <summary>
    /// Contains tests for the database.
    /// </summary>
    [TestFixture]
    public class DatabaseTests
    {
//        /// <summary>
//        /// Tests creating a new node in an empty database.
//        /// </summary>
//        [Test]
//        public void TestEmptyCreateNode()
//        {
//            FileSystemFacade.FileSystem = new Mocks.MockFileSystem(@"/");
//            FileSystemFacade.FileSystem.CreateDirectory(@"/db");
//
//            var db = new Database(@"/db", new MemoryIdGenerator());
//            var node = new Node()
//            {
//                Id = -1,
//                ChangeSetId = 2,
//                Latitude = 50,
//                Longitude = 4,
//                UserId = 1,
//                UserName = "Ben",
//                Visible = true,
//                TimeStamp = DateTime.Now,
//                Version = 1
//            };
//            db.CreateNode(node);
//
//            // make sure it persisted by createing a new db.
//            db = new Database(@"/db", new MemoryIdGenerator());
//            var dbNode = db.GetNode(-1);
//            Assert.IsNotNull(dbNode);
//            Assert.AreEqual(node.Id, dbNode.Id);
//            Assert.AreEqual(node.ChangeSetId, dbNode.ChangeSetId);
//            Assert.AreEqual(node.Latitude, dbNode.Latitude);
//            Assert.AreEqual(node.Longitude, dbNode.Longitude);
//            Assert.AreEqual(node.UserId, dbNode.UserId);
//            Assert.AreEqual(node.UserName, dbNode.UserName);
//            Assert.AreEqual(node.Version, dbNode.Version);
//            Assert.AreEqual(node.Visible, dbNode.Visible);
//            Assert.AreEqual(node.TimeStamp, dbNode.TimeStamp);
//        }
        
        // /// <summary>
        // /// Tests creating a new way in an empty database.
        // /// </summary>
        // [Test]
        // public void TestEmptyCreateWay()
        // {
        //     FileSystemFacade.FileSystem = new Mocks.MockFileSystem(@"/");
        //     FileSystemFacade.FileSystem.CreateDirectory(@"/db");

        //     var db = new Database(@"/db", new MemoryIdGenerator());
        //     db.CreateNode(new Node()
        //     {
        //         Id = -1,
        //         ChangeSetId = 2,
        //         Latitude = 50,
        //         Longitude = 4,
        //         UserId = 1,
        //         UserName = "Ben",
        //         Visible = true,
        //         TimeStamp = DateTime.Now,
        //         Version = 1
        //     });
        //     db.CreateNode(new Node()
        //     {
        //         Id = -2,
        //         ChangeSetId = 2,
        //         Latitude = 50,
        //         Longitude = 4,
        //         UserId = 1,
        //         UserName = "Ben",
        //         Visible = true,
        //         TimeStamp = DateTime.Now,
        //         Version = 1
        //     });
        //     db.CreateWay(new Way()
        //     {
        //         Id = -1,
        //         ChangeSetId = 2,
        //         Nodes = new long[] { -1, -2 },
        //         UserId = 1,
        //         UserName = "Ben",
        //         Visible = true,
        //         TimeStamp = DateTime.Now,
        //         Version = 1
        //     });
        // }
    }
}
