using XeonCommon.Network;

namespace XeonCore.Network
{
    public class NetManager : INetManaged<INetClient>
    {
        public event NetEventIncoming NetEventIn;
        public NetQueue Queue = new NetQueue();
        public NetManager()
        {
        }
        public void Dispose()
        {
            Queue.Dispose();
        }
        public void EmitNetEvent(NetEvent<INetClient> e)
        {
            NetEventIn.Invoke(e);
        }
    }
}
