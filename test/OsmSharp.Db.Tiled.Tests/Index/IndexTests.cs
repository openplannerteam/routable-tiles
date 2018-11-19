using NUnit.Framework;
using OsmSharp.Db.Tiled.Indexes;
using System.IO;

namespace OsmSharp.Db.Tiled.Tests
{
    /// <summary>
    /// Contains tests for the index.
    /// </summary>
    [TestFixture]
    public class IndexTests
    {
        /// <summary>
        /// Tests encoding index data.
        /// </summary>
        [Test]
        public void TestEncode()
        {
            ulong encoded;
            Index.Encode(102834, 45, out encoded);
            Assert.True(0x002d0000000191b2 == encoded);

            Indexes.Index.Encode(102834, 65536 - 1, out encoded);
            Assert.True(0xffff0000000191b2 == encoded);

            Indexes.Index.Encode(-102834, 65536 - 1, out encoded);
            Assert.True(0xffff8000000191b2 == encoded);

            Indexes.Index.Encode(((1L << 47) - 1), 65536 - 1, out encoded);
            Assert.True(0xffff7fffffffffff == encoded);
            
            Indexes.Index.Encode(-((1L << 47) - 1), 65536 - 1, out encoded);
            Assert.True(0xffffffffffffffff == encoded);
        }

        /// <summary>
        /// Tests decoding index data.
        /// </summary>
        [Test]
        public void TestDecode()
        {
            long id;
            int mask;
            Indexes.Index.Decode(0x002d0000000191b2, out id, out mask);
            Assert.AreEqual(102834, id);
            Assert.AreEqual(45, mask);
            Indexes.Index.Decode(0xffff0000000191b2, out id, out mask);
            Assert.AreEqual(102834, id);
            Assert.AreEqual(65536 - 1, mask);
            Indexes.Index.Decode(0xffff8000000191b2, out id, out mask);
            Assert.AreEqual(-102834, id);
            Assert.AreEqual(65536 - 1, mask);
            Indexes.Index.Decode(0xffff7fffffffffff, out id, out mask);
            Assert.AreEqual(((1L << 47) - 1), id);
            Assert.AreEqual(65536 - 1, mask);
            Indexes.Index.Decode(0xffffffffffffffff, out id, out mask);
            Assert.AreEqual(-((1L << 47) - 1), id);
            Assert.AreEqual(65536 - 1, mask);
        }

        /// <summary>
        /// Tests try getting a mask.
        /// </summary>
        [Test]
        public void TestTryGetMask()
        {
            var index = new Index();

            index.Add(1, 65536 - 1);
            index.Add(10, 65536 - 10);
            index.Add(100, 65536 - 100);
            index.Add(1000, 65536 - 1000);
            index.Add(10000, 65536 - 10000);

            int mask;
            Assert.True(index.TryGetMask(1, out mask));
            Assert.AreEqual(65536 - 1, mask);
            Assert.True(index.TryGetMask(10, out mask));
            Assert.AreEqual(65536 - 10, mask);
            Assert.True(index.TryGetMask(100, out mask));
            Assert.AreEqual(65536 - 100, mask);
            Assert.True(index.TryGetMask(1000, out mask));
            Assert.AreEqual(65536 - 1000, mask);
            Assert.True(index.TryGetMask(10000, out mask));
            Assert.AreEqual(65536 - 10000, mask);
        }

        /// <summary>
        /// Tests serializing.
        /// </summary>
        [Test]
        public void TestSerialize()
        {
            var index = new Index();

            index.Add(1, 65536 - 1);
            index.Add(10, 65536 - 10);
            index.Add(100, 65536 - 100);
            index.Add(1000, 65536 - 1000);
            index.Add(10000, 65536 - 10000);

            // SIZE: 8 (size) + 8 * 5 (data).
            using (var stream = new MemoryStream())
            {
                var size = index.Serialize(stream);
                Assert.AreEqual(8 * 6, size);
                Assert.AreEqual(8 * 6, stream.Position);
            }
        }

        /// <summary>
        /// Tests deserializing.
        /// </summary>
        [Test]
        public void TestDeserialize()
        {
            var index = new Index();

            index.Add(1, 65536 - 1);
            index.Add(10, 65536 - 10);
            index.Add(100, 65536 - 100);
            index.Add(1000, 65536 - 1000);
            index.Add(10000, 65536 - 10000);

            // SIZE: 8 (size) + 8 * 5 (data).
            int mask;
            using (var stream = new MemoryStream())
            {
                var size = index.Serialize(stream);

                stream.Seek(0, SeekOrigin.Begin);
                index = Index.Deserialize(stream, Reminiscence.Arrays.ArrayProfile.NoCache);

                Assert.True(index.TryGetMask(1, out mask));
                Assert.AreEqual(65536 - 1, mask);
                Assert.True(index.TryGetMask(10, out mask));
                Assert.AreEqual(65536 - 10, mask);
                Assert.True(index.TryGetMask(100, out mask));
                Assert.AreEqual(65536 - 100, mask);
                Assert.True(index.TryGetMask(1000, out mask));
                Assert.AreEqual(65536 - 1000, mask);
                Assert.True(index.TryGetMask(10000, out mask));
                Assert.AreEqual(65536 - 10000, mask);

                stream.Seek(0, SeekOrigin.Begin);
                index = Index.Deserialize(stream);
            }
            
            Assert.True(index.TryGetMask(1, out mask));
            Assert.AreEqual(65536 - 1, mask);
            Assert.True(index.TryGetMask(10, out mask));
            Assert.AreEqual(65536 - 10, mask);
            Assert.True(index.TryGetMask(100, out mask));
            Assert.AreEqual(65536 - 100, mask);
            Assert.True(index.TryGetMask(1000, out mask));
            Assert.AreEqual(65536 - 1000, mask);
            Assert.True(index.TryGetMask(10000, out mask));
            Assert.AreEqual(65536 - 10000, mask);
        }
    }
}
