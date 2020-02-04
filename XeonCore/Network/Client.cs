using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace XeonCore.Network
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
