using System;
using System.IO;
using Reminiscence.Arrays;
using Reminiscence.IO;
using Reminiscence.IO.Streams;
using System.Collections.Generic;

namespace OsmSharp.Db.Tiled.Indexes
{
    /// <summary>
    /// Represents an index matching id's to one or more subtiles.
    /// </summary>
    public class Index
    {
        private readonly ArrayBase<ulong> _data;
        private const int SUBTILES = 16;
        
        /// <summary>
        /// Creates a new index.
        /// </summary>
        public Index()
        {
            _data = new MemoryArray<ulong>(1024);

            this.IsDirty = false;
        }
        
        private Index(ArrayBase<ulong> data)
        {
            _data = data;
            _pointer = _data.Length;

            this.IsDirty = false;
        }

        private long _pointer = 0;

        /// <summary>
        /// Returns true if the data in this index wasn't saved to disk.
        /// </summary>
        /// <returns></returns>
        public bool IsDirty
        {
            get;
            private set;
        }
        
        /// <summary>
        /// Adds a new entry in this index.
        /// </summary>
        public void Add(long id, int mask)
        {
            if (mask > MAX_MASK) {throw new ArgumentOutOfRangeException(nameof(mask));}

            if (_pointer > 0)
            {
                int previousMask;
                long previousId;
                Decode(_data[_pointer - 1], out previousId, out previousMask);

                if (id < previousId)
                {
                    throw new ArgumentException("Id is smaller than the previous one, id's should be added in ascending order.");
                }
            }

            _data.EnsureMinimumSize(_pointer + 1);
            ulong data;
            Encode(id, mask, out data);
            _data[_pointer] = data;
            _pointer++;

            this.IsDirty = true;
        }

        /// <summary>
        /// Trims the internal data structure to it's minimum size.
        /// </summary>
        public void Trim()
        {
            _data.Resize(_pointer);

            this.IsDirty = true;
        }

        /// <summary>
        /// Tries to get the mask for the given id.
        /// </summary>
        public bool TryGetMask(long id, out int mask)
        {
            return Search(id, out mask) != -1;
        }

        /// <summary>
        /// Serializes this index to the given stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public long Serialize(Stream stream)
        {
            this.Trim();

            var size = _data.Length * 8 + 8;
            stream.Write(BitConverter.GetBytes(_data.Length), 0, 8);
            _data.CopyTo(stream);

            this.IsDirty = false;

            return size;
        }

        /// <summary>
        /// Deserializes an index from the given stream.
        /// </summary>
        public static Index Deserialize(Stream stream, ArrayProfile profile = null)
        {
            var bytes = new byte[8];
            stream.Read(bytes, 0, 8);
            var size = BitConverter.ToInt64(bytes, 0);

            ArrayBase<ulong> data;
            if (profile == null)
            { // just create arrays and read the data.
                data = new MemoryArray<ulong>(size);
                data.CopyFrom(stream);
            }
            else
            { // create accessors over the exact part of the stream that represents vertices/edges.
                var position = stream.Position;
                var map1 = new MemoryMapStream(new CappedStream(stream, position, size * 8));
                data = new Array<ulong>(map1.CreateUInt64(size), profile);
            }

            return new Index(data);
        }

        private const long MAX_MASK = 65536 - 1;
        private const ulong MAX_ID = ((ulong)1 << 47) - 1;
        private const long ID_MASK = ~(MAX_MASK << (64 - SUBTILES));

        /// <summary>
        /// Decodes an id and a mask.
        /// </summary>
        public static void Decode(ulong data, out long id, out int mask)
        {
            mask = (int)(data >> (64 - SUBTILES));

            ulong idmask = 0x00007fffffffffff;
            id = (long)(data & idmask);
            if ((((ulong)1 << 47) & data) != 0)
            {
                id = -id;
            }
        }

        /// <summary>
        /// Encodes an id and a mask.
        /// </summary>
        public static void Encode(long id, int mask, out ulong data)
        {
            if (mask > MAX_MASK) {throw new ArgumentOutOfRangeException(nameof(mask));}
            
            var unsignedId = unchecked((ulong)id);
            if (id < 0)
            {
                unsignedId = unchecked((ulong)-id);
            }

            if (unsignedId > MAX_ID) { throw new ArgumentOutOfRangeException(nameof(id)); }

            // left 16-bits are the mask.
            var masked = (ulong)mask << (64 - SUBTILES);
            // right 48-bits are signed id.
            // copy 47 left bits (leave 48 for sign).
            ulong idmask = 0x0000ffffffffffff;
            var id48 = unsignedId & idmask;
            // set sign.
            if (id < 0)
            {
                id48 = id48 | ((ulong)1 << 47);
            }
            data = id48 | masked;
        }

        private long Search(long id, out int mask)
        {
            var min = 0L;
            var max = _pointer - 1;
            
            long minId;
            Decode(_data[min], out minId, out mask);
            if (minId == id)
            {
                return min;
            }
            long maxId;
            Decode(_data[max], out maxId, out mask);
            if (maxId == id)
            {
                return max;
            }

            while (true)
            {
                var mid = (min + max) / 2;
                long midId;
                Decode(_data[mid], out midId, out mask);
                if (midId == id)
                {
                    return mid;
                }

                if (midId > id)
                {
                    max = mid;
                }
                else
                {
                    min = mid;
                }

                if (max - min <= 1)
                {
                    return -1;
                }
            }
        }
    }
}