using XeonCore.Network;
using XeonCore.Network.Websocket;
using System.Threading;
using System;

namespace XeonProject
{
    public static class Network
    {
        public static NetManager<WClient> Manager = new NetManager<WClient>();
        public static Thread NetworkThread = new Thread(() =>
            {
                WServer server = new WServer("127.0.0.1", 1337);
                server.Connect += (WClient client) =>
                {
                    Console.WriteLine($"Client connected: {client.Ip}");
                    client.MessageReceived += (string data) =>
                    {
                        NetEvent<WClient> e = new NetEvent<WClient> { Client = client, Payload = data };
                        Manager.Queue.CallNetEvent(e);
                    };
                };

                server.Disconnect += (WClient client) =>
                {
                    using (client)
                    {
                        Console.WriteLine($"Client disconnected: {client.Ip}");
                    }
                };
            });
        public static Thread NetQueueWatcher = new Thread(() =>
            {
                while (true)
                {
                    bool success = Manager.Queue.Poll(out NetEvent<WClient>[] events);
                    if (success)
                    {
                        foreach (NetEvent<WClient> e in events)
                        {
                            Manager.EmitNetEvent(e);
                        }
                    }
                }
            });
        public static void Start()
        {
            NetworkThread.Start();
            NetQueueWatcher.Start();
        }
    }
}