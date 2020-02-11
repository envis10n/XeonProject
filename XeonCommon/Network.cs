using System.Collections.Generic;
using System;

namespace XeonCommon.Network
{
    public interface INetClient : IDisposable
    {
    }
    public class BufferBuilder : IDisposable
    {
        public byte[] InternalBuffer;
        public void Add(byte[] buffer)
        {
            if (InternalBuffer == null)
            {
                InternalBuffer = buffer;
            }
            else
            {
                byte[] temp = (byte[])InternalBuffer.Clone();
                InternalBuffer = new byte[buffer.Length + temp.Length];
                Buffer.BlockCopy(temp, 0, InternalBuffer, 0, temp.Length);
                Buffer.BlockCopy(buffer, 0, InternalBuffer, temp.Length, buffer.Length);
            }
        }
        public byte[] Consume()
        {
            byte[] temp = (byte[])InternalBuffer.Clone();
            InternalBuffer = null;
            return temp;
        }
        public bool CanConsume()
        {
            return InternalBuffer != null && InternalBuffer.Length > 0 && BufUtil.HasEOL(InternalBuffer);
        }
        public void Dispose()
        {
            InternalBuffer = null;
        }
    }
}