using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

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

        private object readMutex = new object();
        private object writeMutex = new object();

        private Hashtable canals = new Hashtable();

        public MuxerStream(Stream baseStream)
        {
            BaseStream = baseStream;
        }

        public CanalStream GetCanal(string name, bool createIfNeeded = true)
        {
            lock (writeMutex)
            {
                int canalHash = name.GetHashCode();

                if (canals.ContainsKey(canalHash))
                    return canals[canalHash] as CanalStream;

                byte[] canalHashBytes = BitConverter.GetBytes(canalHash);

                BaseStream.WriteByte((byte)MuxerCommand.CreateCanal);
                BaseStream.Write(canalHashBytes, 0, canalHashBytes.Length);

                if (AutoFlush)
                    BaseStream.Flush();

                CanalStream canal = new CanalStream(this, canalHash);
                canals.Add(canalHash, canal);
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
            }
        }

        internal void Read()
        {
            byte[] header = new byte[16];
            
            lock (readMutex)
            {
                BaseStream.Read(header, 0, 5);

                MuxerCommand command = (MuxerCommand)header[0];
                int canalHash = BitConverter.ToInt32(header, 1);

                switch (command)
                {
                    case MuxerCommand.CreateCanal:
                        CanalStream canal = new CanalStream(this, canalHash);
                        canals.Add(canalHash, canal);

                        break;

                    case MuxerCommand.DestroyCanal:
                        canals.Remove(canalHash);

                        break;

                    case MuxerCommand.Packet:
                        BaseStream.Read(header, 5, 4);
                        int block = BitConverter.ToInt32(header, 5);

                        CanalStream canalStream = canals[canalHash] as CanalStream;
                        byte[] buffer = new byte[block];

                        BaseStream.Read(buffer, 0, block);
                        canalStream.readBuffer.Write(buffer, 0, block);

                        break;
                }
            }
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