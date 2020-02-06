using System.Threading.Tasks;
using System;

namespace XeonCommon.Network
{
    public interface IClient : IDisposable
    {
        public delegate void OnMessageReceived(string data);
        public event OnMessageReceived MessageReceived;
        public Guid GUID { get; }
        public void EmitMessageReceived(string data);
        public Task<bool> Send(string data);
        public void Close();
        public void Close(int code, string reason);
    }
}