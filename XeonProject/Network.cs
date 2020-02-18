using XeonCore.Network;
using System.Threading;
using XeonCommon.Network;

namespace XeonProject
{
    static class Network
    {
        public static NetManager Manager = new NetManager();
        public static Thread NetQueueWatcher = new Thread(() =>
            {
                while (true)
                {
                    bool success = Manager.Queue.Poll(out NetEvent<INetClient>[] events);
                    if (success)
                    {
                        foreach (NetEvent<INetClient> e in events)
                        {
                            Manager.EmitNetEvent(e);
                        }
                    }
                }
            });
        public static void Start()
        {
            NetQueueWatcher.Start();
        }
    }
}