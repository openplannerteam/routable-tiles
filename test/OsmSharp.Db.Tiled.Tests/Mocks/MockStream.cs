using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OsmSharp.Db.Tiled.Tests.Mocks
{
    class MockStream : Stream
    {
        private readonly Mocks.MockFileSystem.File _file;
        private readonly MemoryStream _stream;

        public MockStream(Mocks.MockFileSystem.File file)
        {
            _file = file;
            _stream = new MemoryStream();
            _stream.Write(file.Data, 0, file.Data.Length);
            _stream.Seek(0, SeekOrigin.Begin);
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => _stream.Length;

        public override long Position
        {
            get => _stream.Position;
            set => _stream.Position = value;
        }

        public override void Flush()
        {
            _file.Data = _stream.ToArray();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            this.Flush();

            base.Dispose(disposing);
        }
    }
}