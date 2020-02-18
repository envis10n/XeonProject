using XeonCore.Network;
using XeonNet.Sockets;
using XeonNet;
using System.Threading;
using System;
using XeonCore.Game;
using XeonStorage;

namespace XeonProject
{
    static class Network
    {
        public static NetManager<XeonClient> Manager = new NetManager<XeonClient>();
        public static Thread NetworkThread = new Thread(() =>
            {
                XeonServer server = new XeonServer(Program.Config.Network.Port, Program.Config.Network.Address);
                XeonCommon.Logger Log = server.Log;
                server.OnClientConnect += (XeonClient client) =>
                {
                    Guid guid = Guid.Parse(client.GUID.ToString());
                    Collection actors = DataStorage.Database.GetCollection("actors");
                    Actor player;
                    if (actors != null)
                    {
                        Document playerDoc = new Document();
                        playerDoc.Add(guid.ToString(), new Actor());
                        if (playerDoc.TryGetValue(guid.ToString(), out object pobj))
                        {
                            player = (Actor)pobj;
                            actors.Add(playerDoc);
                            DataStorage.Database.Save();
                        }
                    }
                    client.OnMessage += (string data) =>
                    {
                        NetEvent<XeonClient> e = new NetEvent<XeonClient> { Client = client, Guid = guid, IsDisconnect = false, Payload = data };
                        Manager.Queue.CallNetEvent(e);
                    };
                    client.OnDisconnect += () =>
                    {
                        if (actors != null)
                        {
                            actors.RemoveMatching(d => d.ContainsKey(guid.ToString()));
                            DataStorage.Database.Save();
                        }
                        NetEvent<XeonClient> e = new NetEvent<XeonClient> { Guid = guid, IsDisconnect = true, Client = null, Payload = null };
                        Manager.Queue.CallNetEvent(e);
                    };
                    client.OnTelnetDo += (option) =>
                    {
                        switch (option)
                        {
                            case Telnet.Option.GMCP:
                                return true;
                            case Telnet.Option.LineMode:
                                return true;
                        }
                        return false;
                    };
                    client.OnTelnetWill += (option) =>
                    {
                        switch (option)
                        {
                            case Telnet.Option.LineMode:
                                return true;
                        }
                        return false;
                    };
                    client.OnTelnetSB += (packet) =>
                    {
                        Log.WriteLine($"Unhandled SB from {guid}: {packet.Option} {System.Text.Encoding.UTF8.GetString(packet.Payload)}");
                    };
                    client.OnGMCP += (gmcp) =>
                    {
                        Log.WriteLine($"GMCP from {guid}:\n{gmcp}");
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