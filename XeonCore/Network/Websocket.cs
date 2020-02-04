using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WatsonWebsocket;

namespace XeonCore.Network.Websocket
{
    public class WServer
    {
        public delegate void WSClientConnect(WClient client);
        public delegate void WSClientDisconnect(WClient client);
        public event WSClientConnect Connect;
        public event WSClientDisconnect Disconnect;
        public WatsonWsServer Server;
        public readonly List<WClient> Clients = new List<WClient>();
        public WServer(string hostname, int port)
        {
            Server = new WatsonWsServer(hostname, port, false);
            Server.ClientConnected = ConnectFunc;
            async Task<bool> ConnectFunc(string ipPort, HttpListenerRequest req)
            {
                Console.WriteLine("Debug Client Connect");
                WClient client = new WClient(this, ipPort);
                Clients.Add(client);
                Connect(client);
                return true;
            }
            Server.ClientDisconnected = DisconnectFunc;
            async Task DisconnectFunc(string ipPort)
            {
                WClient client = GetClient(ipPort);
                Clients.Remove(client);
                Disconnect.Invoke(client);
            }
            Server.MessageReceived = MessageFunc;
            async Task MessageFunc(string ipPort, byte[] data)
            {
                WClient client = GetClient(ipPort);
                string message = Encoding.UTF8.GetString(data);
                client.EmitMessageReceived(message);
            }
            Server.Start();
            Console.WriteLine($"WS Server listening on {hostname}:{port}");
        }
        public WClient GetClient(string ipPort)
        {
            return Clients.Find(x => x.Ip == ipPort);
        }
    }
    public class WClient : IClient
    {
        private Guid _guid = Guid.NewGuid();
        public Guid GUID { get => _guid; }
        public event IClient.OnMessageReceived MessageReceived;
        private WServer _server;
        public string Ip { get; }
        public WClient(WServer server, string ipPort)
        {
            _server = server;
            Ip = ipPort;
        }
        public void EmitMessageReceived(string data)
        {
            MessageReceived.Invoke(data);
        }
        public async Task<bool> Send(string data)
        {
            return await _server.Server.SendAsync(Ip, Encoding.UTF8.GetBytes(data));
        }
        public void Dispose()
        {

        }
        public void Close()
        {

        }
        public void Close(int code, string reason)
        {

        }
    }
}
