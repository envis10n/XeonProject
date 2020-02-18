using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace XeonCommon.Network
{
    public delegate void NetEventIncoming(NetEvent<INetClient> e);
    public delegate void NetEventEnqueue(NetEvent<INetClient> e);
    public struct NetEvent<T> where T : INetClient
    {
        public T Client;
        public Guid Guid;
        public string Payload;
        public bool IsDisconnect;
    }
    public interface INetManaged<T> where T : INetClient
    {
        public event NetEventIncoming NetEventIn;
        public void EmitNetEvent(NetEvent<T> e);
    }
    public interface INetQueue<T> : IDisposable where T : INetClient
    {
        public event NetEventEnqueue OnNetEvent;
        public void Enqueue(NetEvent<T> e);
        public void CallNetEvent(NetEvent<T> e);
        public bool Poll(out NetEvent<T>[] result);
    }
    public interface INetClient : IDisposable
    {
        public Task WriteLine(string data);
        public Task Write(string data);
        public Task<string> Prompt(string prompt);
        public void Close();
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