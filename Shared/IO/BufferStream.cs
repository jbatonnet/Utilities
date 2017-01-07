using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Utilities.IO
{
    public class BufferStream : Stream
    {
        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }
        public override bool CanRead
        {
            get
            {
                return true;
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
                return bufferLength;
            }
        }
        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        private Queue<byte[]> blocks = new Queue<byte[]>();
        private int bufferPosition = 0;
        private int bufferLength = 0;

        private object mutex = new object();
        private EventWaitHandle readEvent = new AutoResetEvent(false);

        public override void Flush()
        {
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
            if (bufferLength == 0)
                readEvent.WaitOne();

            int total = 0;

            lock (mutex)
            {
                while (count > 0)
                {
                    if (bufferLength == 0)
                        return total;

                    byte[] block = blocks.Peek();
                    int blockSize = Math.Min(count, block.Length - bufferPosition);

                    Array.Copy(block, bufferPosition, buffer, offset, blockSize);

                    count -= blockSize;
                    bufferLength -= blockSize;
                    offset += blockSize;
                    total += blockSize;

                    blocks.Dequeue();
                }
            }

            return total;
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (count == 0)
                return;

            byte[] block = new byte[count];
            Array.Copy(buffer, offset, block, 0, count);

            lock (mutex)
            {
                blocks.Enqueue(block);
                bufferLength += count;
            }

            readEvent.Set();
        }
    }
}