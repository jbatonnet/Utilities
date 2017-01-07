using System;
using System.Collections.Concurrent;
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
                return queue.Count;
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

        private ConcurrentQueue<byte> queue = new ConcurrentQueue<byte>();

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
            if (count == 0)
                return 0;

            for (int i = 0; i < count; i++)
            {
                byte value;

                while (!queue.TryDequeue(out value))
                    readEvent.WaitOne();

                buffer[offset + i] = value;
            }

            return count;
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (count == 0)
                return;

            for (int i = 0; i < count; i++)
                queue.Enqueue(buffer[offset + i]);

            readEvent.Set();
        }
    }
}