using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using XeonCommon.Threads;
using XeonCommon;
using XeonCommon.Network;
using System.Threading;
using System.Threading.Tasks;

namespace XeonNet.Sockets
{
    public class XeonClient : INetClient
    {
        public readonly Guid GUID = Guid.NewGuid();
        public readonly string RemoteEndPoint;
        public event Action<string> OnMessage;
        public event Action OnDisconnect;
        public event Action<Telnet.TelnetPacket> OnTelnet;
        public event Action<byte> OnTelnetWill;
        public event Action<byte> OnTelnetWont;
        public event Action<byte> OnTelnetDo;
        public event Action<byte> OnTelnetDont;
        public event Action<GMCP.GmcpData> OnGMCP;
        public event Action<Telnet.TelnetPacket> OnTelnetSB;
        public event Action<Telnet.TelnetPacket> OnTelnetUnhandled;
        private byte[] buffer;
        private TcpClient Client;
        private NetworkStream Stream;
        public WrapMutex<List<byte>> Options = new WrapMutex<List<byte>>(new List<byte>());
        public XeonClient(TcpClient client)
        {
            Client = client;
            RemoteEndPoint = Client.Client.RemoteEndPoint.ToString();
            Stream = Client.GetStream();

            // Handle Telnet events internally first.
            OnTelnet += (packet) =>
            {
                switch (packet.Command)
                {
                    case Telnet.Command.SB:
                        switch (packet.Option)
                        {
                            case Telnet.Option.GMCP:
                                GMCP.GmcpData gmcp = GMCP.GmcpData.FromTelnetPacket(packet);
                                InvokeOnGMCP(gmcp);
                                break;
                            default:
                                InvokeOnTelnetSB(packet);
                                break;
                        }
                        break;
                    case Telnet.Command.WILL:
                        InvokeOnTelnetWill(packet.Option);
                        break;
                    case Telnet.Command.WONT:
                        InvokeOnTelnetWont(packet.Option);
                        break;
                    case Telnet.Command.DO:
                        InvokeOnTelnetDo(packet.Option);
                        break;
                    case Telnet.Command.DONT:
                        InvokeOnTelnetDont(packet.Option);
                        break;
                    default:
                        InvokeOnTelnetUnhandled(packet);
                        break;
                }
            };

            Task t = new Task(async () =>
            {
                while (true)
                {
                    try
                    {
                        Client.Client.Send(new byte[1], 0, 0);
                        BufferBuilder builder = new BufferBuilder();
                        int bytesRead = 0;
                        while (Stream.DataAvailable)
                        {
                            buffer = new byte[1024];
                            int localBytes = await Stream.ReadAsync(buffer, 0, buffer.Length);
                            bytesRead += localBytes;
                            if (buffer.Length > localBytes)
                            {
                                byte[] temp = new byte[localBytes];
                                Buffer.BlockCopy(buffer, 0, temp, 0, localBytes);
                                builder.Add(temp);
                            }
                            else
                            {
                                builder.Add(buffer);
                            }
                        }
                        if (bytesRead > 0)
                        {
                            byte[] buf = builder.Consume();
                            builder = null;
                            if (BufUtil.CountDelim(buf, Telnet.IAC) > 0)
                            {
                                List<Telnet.TelnetPacket> packets = Telnet.Parse(buf, out byte[] remaining);
                                packets.ForEach(packet =>
                                {
                                    InvokeOnTelnet(packet);
                                });
                                if (remaining.Length > 0)
                                {
                                    string data = Encoding.UTF8.GetString(remaining);
                                    InvokeOnMessage(data);
                                }
                            }
                            else
                            {
                                string data = Encoding.UTF8.GetString(buf);
                                InvokeOnMessage(data);
                            }
                        }
                    }
                    catch (SocketException)
                    {
                        break;
                    }
                }
                Client.Close();
                InvokeOnDisconnect();
            });
            t.Start();
        }
        public bool HasOption(byte option)
        {
            using var opts = Options.Lock();
            return opts.Value.Contains(option);
        }
        public void ToggleOption(byte option)
        {
            using var opts = Options.Lock();
            if (opts.Value.Contains(option))
            {
                opts.Value.Remove(option);
            } else
            {
                opts.Value.Add(option);
            }
        }
        public bool SetOption(byte option)
        {
            using var opts = Options.Lock();
            if (opts.Value.Contains(option))
                return false;
            opts.Value.Add(option);
            return true;
        }
        public bool RemoveOption(byte option)
        {
            using var opts = Options.Lock();
            if (!opts.Value.Contains(option))
                return false;
            opts.Value.Remove(option);
            return true;
        }
        public async Task<bool> Will(byte option)
        {
            if (SetOption(option))
            {
                await SendTelnet(Telnet.Command.WILL, option);
                return true;
            }
            else
                return false;
        }
        public async Task<bool> Wont(byte option)
        {
            if (RemoveOption(option))
            {
                await SendTelnet(Telnet.Command.WONT, option);
                return true;
            }
            else
                return false;
        }
        public async Task<bool> Do(byte option)
        {
            if (SetOption(option))
            {
                await SendTelnet(Telnet.Command.DO, option);
                return true;
            }
            else
                return false;
        }
        public async Task<bool> Dont(byte option)
        {
            if (RemoveOption(option))
            {
                await SendTelnet(Telnet.Command.DONT, option);
                return true;
            }
            else
                return false;
        }
        public async Task SendTelnet(byte command, byte option)
        {
            try
            {
                byte[] t = Telnet.CreateTelnetData(command, option);
                await Stream.WriteAsync(t, 0, t.Length);
            } catch (Exception)
            {
                //
            }
        }
        public async Task SendGMCP(string path, Dictionary<string, object> payload)
        {
            try
            {
                byte[] t = Telnet.CreateGMCPData(path, payload);
                await Stream.WriteAsync(t, 0, t.Length);
            } catch (Exception)
            {
                //
            }
        }
        public async Task WriteLine(string data)
        {
            try { 
                byte[] buffer = Encoding.UTF8.GetBytes(data+"\n\r");
                await Stream.WriteAsync(buffer, 0, buffer.Length);
            } catch (Exception)
            {
                //
            }
        }
        public void Dispose()
        {
            Client.Dispose();
        }
        public void InvokeOnTelnetUnhandled(Telnet.TelnetPacket packet)
        {
            if (OnTelnetUnhandled != null)
                OnTelnetUnhandled.Invoke(packet);
        }
        public void InvokeOnTelnetWill(byte option)
        {
            if (OnTelnetWill != null)
                OnTelnetWill.Invoke(option);
        }
        public void InvokeOnTelnetWont(byte option)
        {
            if (OnTelnetWont != null)
                OnTelnetWont.Invoke(option);
        }
        public void InvokeOnTelnetDo(byte option)
        {
            if (OnTelnetDo != null)
                OnTelnetDo.Invoke(option);
        }
        public void InvokeOnTelnetDont(byte option)
        {
            if (OnTelnetDont != null)
                OnTelnetDont.Invoke(option);
        }
        public void InvokeOnTelnetSB(Telnet.TelnetPacket packet)
        {
            if (OnTelnetSB != null)
                OnTelnetSB.Invoke(packet);
        }
        public void InvokeOnGMCP(GMCP.GmcpData data)
        {
            if (OnGMCP != null)
                OnGMCP.Invoke(data);
        }
        public void InvokeOnTelnet(Telnet.TelnetPacket packet)
        {
            if (OnTelnet != null)
                OnTelnet.Invoke(packet);
        }
        public void InvokeOnDisconnect()
        {
            if (OnDisconnect != null)
                OnDisconnect.Invoke();
        }
        public void InvokeOnMessage(string data)
        {
            if (OnMessage != null)
                OnMessage.Invoke(data);
        }
    }
    public class XeonServer
    {
        public delegate void HadClientConnect(XeonClient client);
        public event HadClientConnect OnClientConnect;
        public Logger Log = new Logger("[Network]");
        public readonly int Port;
        public readonly IPAddress BindAddress;
        private TcpListener Listener;
        public readonly WrapMutex<List<XeonClient>> Clients = new WrapMutex<List<XeonClient>>(new List<XeonClient>());
        public Thread NetworkThread;
        public XeonServer(int port, IPAddress bindAddress)
        {
            Port = port;
            BindAddress = bindAddress;
        }
        public XeonServer(int port, string bindAddress)
        {
            Port = port;
            BindAddress = bindAddress == "0.0.0.0" ? IPAddress.Any : IPAddress.Parse(bindAddress);
        }
        public XeonServer(int port)
        {
            Port = port;
            BindAddress = IPAddress.Loopback;
        }
        public void InvokeClientConnect(XeonClient client)
        {
            if (OnClientConnect != null)
                OnClientConnect.Invoke(client);
        }
        public void Start()
        {
            Listener = new TcpListener(BindAddress, Port);
            Listener.Start();
            Log.WriteLine($"Listening on {BindAddress}:{Port}...");
            NetworkThread = new Thread(async () =>
            {
                while (true)
                {
                    if (Listener.Pending())
                    {
                        TcpClient tclient = await Listener.AcceptTcpClientAsync();
                        XeonClient xclient = new XeonClient(tclient);
                        xclient.OnMessage += (data) =>
                        {
                            Log.WriteLine($"Client <{xclient.GUID}> data received: {data}");
                        };
                        xclient.OnDisconnect += () =>
                        {
                            using (MutLock<List<XeonClient>> list = Clients.Lock())
                            {
                                list.Value.Remove(xclient);
                            }
                            Log.WriteLine($"Client <{xclient.GUID}> disconnected.");
                            xclient = null;
                            tclient = null;
                        };
                        using (MutLock<List<XeonClient>> list = Clients.Lock())
                        {
                            list.Value.Add(xclient);
                        }
                        Log.WriteLine($"Client <{xclient.GUID}> connected: {xclient.RemoteEndPoint}");
                        xclient.ToggleOption(Telnet.Option.GMCP);
                        await xclient.SendTelnet(Telnet.Command.WILL, Telnet.Option.GMCP);
                        InvokeClientConnect(xclient);
                    }
                }
            });
            NetworkThread.Start();
        }
    }
}
