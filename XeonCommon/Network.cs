using System.Collections.Generic;
using System;

namespace XeonCommon.Network
{
    public interface INetClient : IDisposable
    {
    }
    public class BufferBuilder : IDisposable
    {
        List<byte[]> Buffers = new List<byte[]>();
        public void Add(byte[] buffer)
        {
            Buffers.Add(buffer);
        }
        public byte[] Consume()
        {
            int size = 0;
            int pointer = 0;
            Buffers.ForEach(buf =>
            {
                size += buf.Length;
            });
            byte[] buffer = new byte[size];
            Buffers.ForEach(buf =>
            {
                Buffer.BlockCopy(buf, 0, buffer, pointer, buf.Length);
                pointer += buf.Length;
            });
            Dispose();
            return buffer;
        }
        public void Dispose()
        {
            Buffers.Clear();
            Buffers = null;
        }
    }
}