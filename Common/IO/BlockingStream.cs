using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Utilities.IO
{
    public class BlockingStream : Stream
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

        public BlockingStream(Stream baseStream)
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
            while (count > 0)
            {
                int result = BaseStream.Read(buffer, offset, count);
                if (result <= 0)
                {
                    Thread.Sleep(10);
                    continue;
                }

                count -= result;
                offset += result;
            }

            return count;
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            BaseStream.Write(buffer, offset, count);
        }
    }
}