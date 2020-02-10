using XeonCore.Network;
using XeonNet.Sockets;
using XeonNet;
using System.Threading;
using System;

namespace XeonProject
{
    public static class Network
    {
        public static NetManager<XeonClient> Manager = new NetManager<XeonClient>();
        public static Thread NetworkThread = new Thread(() =>
            {
                XeonServer server = new XeonServer(Program.Config.Network.Port, Program.Config.Network.Address);
                server.OnClientConnect += (XeonClient client) =>
                {
                    Guid guid = Guid.Parse(client.GUID.ToString());
                    client.OnMessage += (string data) =>
                    {
                        NetEvent<XeonClient> e = new NetEvent<XeonClient> { Client = client, Guid = guid, IsDisconnect = false, Payload = data };
                        Manager.Queue.CallNetEvent(e);
                    };
                    client.OnDisconnect += () =>
                    {
                        NetEvent<XeonClient> e = new NetEvent<XeonClient> { Guid = guid, IsDisconnect = true, Client = null, Payload = null };
                        Manager.Queue.CallNetEvent(e);
                    };
                    client.OnTelnet += (packet) =>
                    {
                        switch (packet.Command) 
                        {
                            case Telnet.Command.SB:
                                switch (packet.Option)
                                {
                                    case Telnet.Option.GMCP:
                                        GMCP.GmcpData gmcp = GMCP.GmcpData.FromTelnetPacket(packet);
                                        Console.WriteLine($"Client <{guid}> GMCP Packet received:\n{gmcp}");
                                        break;
                                }
                                break;
                            default:
                                Console.WriteLine($"Client <{guid}> telnet event:\n{packet}");
                                break;
                        }
                    };
                };
                server.Start();
            });
        public static Thread NetQueueWatcher = new Thread(() =>
            {
                while (true)
                {
                    bool success = Manager.Queue.Poll(out NetEvent<XeonClient>[] events);
                    if (success)
                    {
                        foreach (NetEvent<XeonClient> e in events)
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