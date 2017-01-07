using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Utilities.IO
{
    public class MeteredStream : Stream
    {
        public Stream BaseStream { get; }

        public override bool CanSeek
        {
            get
            {
                return BaseStream.CanSeek;
            }
        }
        public override bool CanRead
        {
            get
            {
                return BaseStream.CanRead;
            }
        }
        public override bool CanWrite
        {
            get
            {
                return BaseStream.CanWrite;
            }
        }

        public override long Length
        {
            get
            {
                return BaseStream.Length;
            }
        }
        public override long Position
        {
            get
            {
                return BaseStream.Position;
            }
            set
            {
                BaseStream.Position = value;
            }
        }

        public long TotalRead { get; private set; } = 0;
        public long TotalWritten { get; private set; } = 0;

        // TODO: Add read/write rate

        public MeteredStream(Stream baseStream)
        {
            BaseStream = baseStream;
        }

        public override void Flush()
        {
            BaseStream.Flush();
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            return BaseStream.Seek(offset, origin);
        }
        public override void SetLength(long value)
        {
            BaseStream.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int result = BaseStream.Read(buffer, offset, count);

            TotalRead += result;

            return result;
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            BaseStream.Write(buffer, offset, count);

            TotalWritten += count;
        }
    }
}