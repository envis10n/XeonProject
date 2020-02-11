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
        public delegate void HadMessageEvent(string data);
        public delegate void HadTelnetEvent(Telnet.TelnetPacket packet);
        public event HadMessageEvent OnMessage;
        public event Action OnDisconnect;
        public event HadTelnetEvent OnTelnet;
        private byte[] buffer;
        private TcpClient Client;
        private NetworkStream Stream;
        public List<byte> Options = new List<byte>();
        public XeonClient(TcpClient client)
        {
            Client = client;
            RemoteEndPoint = Client.Client.RemoteEndPoint.ToString();
            Stream = Client.GetStream();
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
                                    foreach (string line in data.Split("\n"))
                                    {
                                        if (line.Length > 0)
                                            InvokeOnMessage(line);
                                    }
                                }
                            }
                            else
                            {
                                string data = Encoding.UTF8.GetString(buf);
                                foreach (string line in data.Split("\n"))
                                {
                                    if (line.Length > 0)
                                        InvokeOnMessage(line);
                                }
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
            return Options.Contains(option);
        }
        public void ToggleOption(byte option)
        {
            if (Options.Contains(option))
            {
                Options.Remove(option);
            } else
            {
                Options.Add(option);
            }
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
