using NUnit.Framework;
using OsmSharp.Db.Tiled.Tiles;
using System.Linq;

namespace OsmSharp.Db.Tiled.Tests.Tiles
{
    /// <summary>
    /// Contains tests for the tile extensions.
    /// </summary>
    [TestFixture]
    public class TileExtensionsTests
    {
        /// <summary>
        /// Tests building a mask.
        /// </summary>
        [Test]
        public void TestBuildMask2()
        {
            Assert.AreEqual(1 << 0, (new Tile(0, 0, 2)).BuildMask2());
            Assert.AreEqual(1 << 1, (new Tile(1, 0, 2)).BuildMask2());
            Assert.AreEqual(1 << 2, (new Tile(2, 0, 2)).BuildMask2());
            Assert.AreEqual(1 << 3, (new Tile(3, 0, 2)).BuildMask2());
            Assert.AreEqual(1 << 4, (new Tile(0, 1, 2)).BuildMask2());
            Assert.AreEqual(1 << 5, (new Tile(1, 1, 2)).BuildMask2());
            Assert.AreEqual(1 << 6, (new Tile(2, 1, 2)).BuildMask2());
            Assert.AreEqual(1 << 7, (new Tile(3, 1, 2)).BuildMask2());
            Assert.AreEqual(1 << 8, (new Tile(0, 2, 2)).BuildMask2());
            Assert.AreEqual(1 << 9, (new Tile(1, 2, 2)).BuildMask2());
            Assert.AreEqual(1 << 10, (new Tile(2, 2, 2)).BuildMask2());
            Assert.AreEqual(1 << 11, (new Tile(3, 2, 2)).BuildMask2());
            Assert.AreEqual(1 << 12, (new Tile(0, 3, 2)).BuildMask2());
            Assert.AreEqual(1 << 13, (new Tile(1, 3, 2)).BuildMask2());
            Assert.AreEqual(1 << 14, (new Tile(2, 3, 2)).BuildMask2());
            Assert.AreEqual(1 << 15, (new Tile(3, 3, 2)).BuildMask2());
        }

        /// <summary>
        /// Tests build sub tiles from a mask.
        /// </summary>
        [Test]
        public void TestSubTilesForMask2()
        {
            var tile = new Tile(0, 0, 0);
            Assert.AreEqual(new Tile[] { new Tile(0, 0, 2) }, tile.SubTilesForMask2(1 << 0).ToList());
            Assert.AreEqual(new Tile[] { new Tile(1, 0, 2) }, tile.SubTilesForMask2(1 << 1).ToList());
            Assert.AreEqual(new Tile[] { new Tile(2, 0, 2) }, tile.SubTilesForMask2(1 << 2).ToList());
            Assert.AreEqual(new Tile[] { new Tile(3, 0, 2) }, tile.SubTilesForMask2(1 << 3).ToList());
            Assert.AreEqual(new Tile[] { new Tile(0, 1, 2) }, tile.SubTilesForMask2(1 << 4).ToList());
            Assert.AreEqual(new Tile[] { new Tile(1, 1, 2) }, tile.SubTilesForMask2(1 << 5).ToList());
            Assert.AreEqual(new Tile[] { new Tile(2, 1, 2) }, tile.SubTilesForMask2(1 << 6).ToList());
            Assert.AreEqual(new Tile[] { new Tile(3, 1, 2) }, tile.SubTilesForMask2(1 << 7).ToList());
            
            Assert.AreEqual(new Tile[] { new Tile(0, 0, 2), new Tile(3, 0, 2) }, 
                tile.SubTilesForMask2((1 << 0) + (1 << 3)).ToList());
        }
    }
}