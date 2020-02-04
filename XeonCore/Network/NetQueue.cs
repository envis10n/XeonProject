using System;
using System.Collections.Concurrent;
using System.Text;

namespace XeonCore.Network
{
    public struct NetEvent<T> where T : IClient
    {
        public T Client;
        public string Payload;
    }
    public class NetQueue<T> : IDisposable where T : IClient
    {
        public delegate void NetEventEnqueue(NetEvent<T> e);
        public event NetEventEnqueue OnNetEvent;
        protected static ConcurrentQueue<NetEvent<T>> Queue = new ConcurrentQueue<NetEvent<T>>();
        public NetQueue()
        {
            OnNetEvent += (NetEvent<T> e) =>
            {
                Enqueue(e);
            };
        }
        public void Dispose()
        {
            Queue.Clear();
        }
        public void Enqueue(NetEvent<T> e)
        {
            Queue.Enqueue(e);
        }
        public void CallNetEvent(NetEvent<T> e)
        {
            OnNetEvent.Invoke(e);
        }
        public bool Poll(out NetEvent<T> result)
        {
            return Queue.TryDequeue(out result);
        }
    }
}
