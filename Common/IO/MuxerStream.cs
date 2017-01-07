using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Utilities.IO
{
    public class MuxerStream
    {
        private enum MuxerCommand : byte
        {
            CreateCanal,
            DestroyCanal,
            Packet
        }

        public Stream BaseStream { get; }
        public bool AutoFlush { get; set; } = true;
        public int PacketSize { get; set; } = 4096;

#if DEBUG
        public object Marker { get; set; }
#endif

        internal EventWaitHandle readEvent = new AutoResetEvent(false);
        private object writeMutex = new object();

        private Hashtable canals = new Hashtable();
        private byte[] buffer = new byte[1];

        public MuxerStream(Stream baseStream)
        {
            BaseStream = baseStream;
            baseStream.BeginRead(buffer, 0, 1, MuxerStream_Read, null);
        }

        public CanalStream GetCanal(string name, bool createIfNeeded = true)
        {
            lock (writeMutex)
            {
                int canalHash = name.GetHashCode();

                lock (canals)
                    if (canals.ContainsKey(canalHash))
                        return canals[canalHash] as CanalStream;

                byte[] canalHashBytes = BitConverter.GetBytes(canalHash);

                BaseStream.WriteByte((byte)MuxerCommand.CreateCanal);
                BaseStream.Write(canalHashBytes, 0, canalHashBytes.Length);

                if (AutoFlush)
                    BaseStream.Flush();

                CanalStream canal;

                lock (canals)
                {
                    if (!canals.ContainsKey(canalHash))
                    {
                        canal = new CanalStream(this, canalHash);
                        canals.Add(canalHash, canal);
                    }
                    else
                        canal = canals[canalHash] as CanalStream;
                }

                //Log.Trace("[{0}] Sent CreateCanal 0x{1:X8}", Marker, canalHash);
                return canal;
            }
        }
        internal void DestroyCanal(CanalStream canal)
        {
            lock (writeMutex)
            {
                byte[] canalHashBytes = BitConverter.GetBytes(canal.Hash);

                BaseStream.WriteByte((byte)MuxerCommand.DestroyCanal);
                BaseStream.Write(canalHashBytes, 0, canalHashBytes.Length);

                if (AutoFlush)
                    BaseStream.Flush();

                canals.Remove(canal.Hash);
                //Log.Trace("[{0}] Sent DestroyCanal 0x{1:X8}", Marker, canal.Hash);
            }
        }

        private void MuxerStream_Read(IAsyncResult asyncResult)
        {
            int count = BaseStream.EndRead(asyncResult);

            byte[] header = new byte[8];
            BaseStream.Read(header, 0, 4);

            MuxerCommand command = (MuxerCommand)buffer[0];
            int canalHash = BitConverter.ToInt32(header, 0);

            switch (command)
            {
                case MuxerCommand.CreateCanal:
                    lock (canals)
                        if (!canals.ContainsKey(canalHash))
                        {
                            CanalStream canal = new CanalStream(this, canalHash);
                            canals.Add(canalHash, canal);
                        }

                    //Log.Trace("[{0}] Received CreateCanal 0x{1:X8}", Marker, canalHash);
                    break;

                case MuxerCommand.DestroyCanal:
                    canals.Remove(canalHash);

                    //Log.Trace("[{0}] Received DestroyCanal 0x{1:X8}", Marker, canalHash);
                    break;

                case MuxerCommand.Packet:
                    BaseStream.Read(header, 4, 4);
                    int block = BitConverter.ToInt32(header, 4);

                    CanalStream canalStream = canals[canalHash] as CanalStream;
                    byte[] buffer = new byte[block];

                    BaseStream.Read(buffer, 0, block);
                    canalStream.Buffer.Write(buffer, 0, block);
                    readEvent.Set();

                    //Log.Trace("[{0}][0x{1:X8}] Received {2} bytes", Marker, canalHash, block);
                    break;
            }

            BaseStream.BeginRead(buffer, 0, 1, MuxerStream_Read, null);
        }
        internal void Write(CanalStream canal, byte[] buffer, int offset, int count)
        {
            byte[] canalHashBytes = BitConverter.GetBytes(canal.Hash);

            for (int i = offset; i < offset + count; i+= PacketSize)
            {
                int block = Math.Min(PacketSize, offset + count - i);
                byte[] blockBytes = BitConverter.GetBytes(block);

                lock (writeMutex)
                {
                    BaseStream.WriteByte((byte)MuxerCommand.Packet);
                    BaseStream.Write(canalHashBytes, 0, canalHashBytes.Length);
                    BaseStream.Write(blockBytes, 0, blockBytes.Length);
                    BaseStream.Write(buffer, i, block);

                    //Log.Trace("[{0}][0x{1:X8}] Sent {2} bytes", Marker, canal.Hash, block);

                    if (AutoFlush)
                        BaseStream.Flush();
                }
            }
        }
        internal void Flush()
        {
            BaseStream.Flush();
        }
    }
}