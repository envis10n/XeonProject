using XeonCommon.Network;

namespace XeonCore.Network
{
    public interface INetManaged<T> where T : INetClient
    {
        public delegate void NetEventIncoming(NetEvent<T> e);
        public event NetEventIncoming NetEventIn;
        public void EmitNetEvent(NetEvent<T> e);
    }
    public class NetManager<T> : INetManaged<T> where T : INetClient
    {
        public event INetManaged<T>.NetEventIncoming NetEventIn;
        public NetQueue<T> Queue = new NetQueue<T>();
        public NetManager()
        {
        }
        public void Dispose()
        {
            Queue.Dispose();
        }
        public void EmitNetEvent(NetEvent<T> e)
        {
            NetEventIn.Invoke(e);
        }
    }
}
