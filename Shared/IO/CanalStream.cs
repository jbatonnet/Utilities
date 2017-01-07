using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Utilities.IO
{
    public class CanalStream : Stream
    {
        public MuxerStream Muxer { get; }
        public int Hash { get; }

        internal BufferStream readBuffer = new BufferStream();

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }
        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }
        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }

        public override long Length
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public override long Position
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        internal CanalStream(MuxerStream muxer, int id)
        {
            Muxer = muxer;
            Hash = id;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            while (count > readBuffer.Length)
                Muxer.Read();

            return readBuffer.Read(buffer, offset, count);
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            Muxer.Write(this, buffer, offset, count);
        }
        public override void Flush()
        {
            Muxer.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            Muxer.DestroyCanal(this);

            base.Dispose(disposing);
        }
    }
}