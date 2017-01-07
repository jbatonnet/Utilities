using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Utilities.IO
{
    public class DebugStream : Stream
    {
        public string Name { get; }
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

        public DebugStream(Stream baseStream, string name)
        {
            Name = name;
            BaseStream = baseStream;
        }

        public override void Flush()
        {
            Log.Debug("[{0}] Flush", Name);
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

            string debug = string.Join(" ", buffer.Skip(offset).Take(count).Select(b => b.ToString("X2")));
            Log.Debug("[{0}] Read {1}", Name, debug);

            return result;
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            string debug = string.Join(" ", buffer.Skip(offset).Take(count).Select(b => b.ToString("X2")));
            Log.Debug("[{0}] Write {1}", Name, debug);

            BaseStream.Write(buffer, offset, count);
        }
    }
}