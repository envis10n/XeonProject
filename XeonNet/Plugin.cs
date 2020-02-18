using XeonNet;
using XeonCore.Network;
using XeonNet.Sockets;
using System.Threading;
using XeonCommon;
using XeonCommon.Network;
using System;
using XeonCommon.Config;

public class Plugin : IPlugin
{
    public void Init(ConcurrentDictionary<string, object> globals)
    {
        ProgramConfig config = (ProgramConfig)globals["Config"];
        NetManager Manager = (NetManager)globals["NetManager"];
        Thread network = new Thread(() =>
        {
            XeonServer server = new XeonServer(config.Network.Port, config.Network.Address);
            XeonCommon.Logger Log = server.Log;
            server.OnClientConnect += (XeonClient client) =>
            {
                Guid guid = Guid.Parse(client.GUID.ToString());
                client.OnMessage += (string data) =>
                {
                    NetEvent<INetClient> e = new NetEvent<INetClient> { Client = client, Guid = guid, IsDisconnect = false, Payload = data };
                    Manager.Queue.CallNetEvent(e);
                };
                client.OnDisconnect += () =>
                {
                    NetEvent<INetClient> e = new NetEvent<INetClient> { Guid = guid, IsDisconnect = true, Client = null, Payload = null };
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
        network.Start();
    }
}