using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Collections.Generic;
using XeonCommon.Network;

namespace XeonCore.Network
{
    public struct NetEvent<T> where T : INetClient
    {
        public T Client;
        public Guid Guid;
        public string Payload;
        public bool IsDisconnect;
    }
    public class NetQueue<T> : IDisposable where T : INetClient
    {
        private Mutex Mut = new Mutex();
        public delegate void NetEventEnqueue(NetEvent<T> e);
        public event NetEventEnqueue OnNetEvent;
        protected ConcurrentQueue<NetEvent<T>> Queue = new ConcurrentQueue<NetEvent<T>>();
        public NetQueue()
        {
            OnNetEvent += (NetEvent<T> e) =>
            {
                Mut.WaitOne();
                Enqueue(e);
                Mut.ReleaseMutex();
            };
        }
        public void Dispose()
        {
            Queue.Clear();
        }
        public void Enqueue(NetEvent<T> e)
        {
            Mut.WaitOne();
            Queue.Enqueue(e);
            Mut.ReleaseMutex();
        }
        public void CallNetEvent(NetEvent<T> e)
        {
            OnNetEvent.Invoke(e);
        }
        public bool Poll(out NetEvent<T>[] result)
        {
            List<NetEvent<T>> re = new List<NetEvent<T>>();
            Mut.WaitOne();
            while (Queue.TryDequeue(out NetEvent<T> res))
            {
                re.Add(res);
            }
            Mut.ReleaseMutex();
            result = re.ToArray();
            return result.Length > 0;
        }
    }
}
