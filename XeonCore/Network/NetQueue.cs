using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Collections.Generic;
using XeonCommon.Network;

namespace XeonCore.Network
{
    public class NetQueue : INetQueue<INetClient>
    {
        private Mutex Mut = new Mutex();
        public event NetEventEnqueue OnNetEvent;
        protected ConcurrentQueue<NetEvent<INetClient>> Queue = new ConcurrentQueue<NetEvent<INetClient>>();
        public NetQueue()
        {
            OnNetEvent += (NetEvent<INetClient> e) =>
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
        public void Enqueue(NetEvent<INetClient> e)
        {
            Mut.WaitOne();
            Queue.Enqueue(e);
            Mut.ReleaseMutex();
        }
        public void CallNetEvent(NetEvent<INetClient> e)
        {
            OnNetEvent.Invoke(e);
        }
        public bool Poll(out NetEvent<INetClient>[] result)
        {
            List<NetEvent<INetClient>> re = new List<NetEvent<INetClient>>();
            Mut.WaitOne();
            while (Queue.TryDequeue(out NetEvent<INetClient> res))
            {
                re.Add(res);
            }
            Mut.ReleaseMutex();
            result = re.ToArray();
            return result.Length > 0;
        }
    }
}
