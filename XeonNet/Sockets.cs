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
        public delegate bool TelnetOptionNeg(byte option);
        public readonly Guid GUID = Guid.NewGuid();
        public readonly string RemoteEndPoint;
        public event Action<string> OnMessage;
        public event Action OnDisconnect;
        public event Action<Telnet.TelnetPacket> OnTelnet;
        public event TelnetOptionNeg OnTelnetWill;
        public event TelnetOptionNeg OnTelnetWont;
        public event TelnetOptionNeg OnTelnetDo;
        public event TelnetOptionNeg OnTelnetDont;
        public event Action<GMCP.GmcpData> OnGMCP;
        public event Action<Telnet.TelnetPacket> OnTelnetSB;
        public event Action<Telnet.TelnetPacket> OnTelnetUnhandled;
        private byte[] buffer;
        private TcpClient Client;
        private NetworkStream Stream;
        public WrapMutex<Dictionary<byte, Telnet.TelnetOptionState>> Options = new WrapMutex<Dictionary<byte, Telnet.TelnetOptionState>>(new Dictionary<byte, Telnet.TelnetOptionState>());
        public BufferBuilder ClientBuffer = new BufferBuilder();
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
                        if (!IsWaitingOn(packet.Option))
                        {
                            // Client initiated
                            SetOption(packet.Option, Telnet.TelnetOptionState.Waiting);
                            InvokeOnTelnetWill(packet.Option);
                        }
                        else
                        {
                            // Server initiated.
                            SetOption(packet.Option, Telnet.TelnetOptionState.Enabled);
                        }
                        break;
                    case Telnet.Command.WONT:
                        if (!IsWaitingOn(packet.Option))
                        {
                            // Client initiated
                            SetOption(packet.Option, Telnet.TelnetOptionState.Waiting);
                            InvokeOnTelnetWont(packet.Option);
                        }
                        else
                        {
                            // Server initiated.
                            SetOption(packet.Option, Telnet.TelnetOptionState.Disabled);
                        }
                        break;
                    case Telnet.Command.DO:
                        if (!IsWaitingOn(packet.Option))
                        {
                            // Client initiated
                            SetOption(packet.Option, Telnet.TelnetOptionState.Waiting);
                            InvokeOnTelnetDo(packet.Option);
                        }
                        else
                        {
                            // Server initiated.
                            SetOption(packet.Option, Telnet.TelnetOptionState.Enabled);
                        }
                        break;
                    case Telnet.Command.DONT:
                        if (!IsWaitingOn(packet.Option))
                        {
                            // Client initiated
                            SetOption(packet.Option, Telnet.TelnetOptionState.Waiting);
                            InvokeOnTelnetDont(packet.Option);
                        }
                        else
                        {
                            // Server initiated.
                            SetOption(packet.Option, Telnet.TelnetOptionState.Disabled);
                        }
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
                        Client.Client.Send(new byte[1], 0);
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
                                buffer = temp;
                            }
                            ClientBuffer.Add(buffer);
                            if (BufUtil.CountDelim(ClientBuffer.InternalBuffer, Telnet.IAC) > 0)
                            {
                                List<Telnet.TelnetPacket> packets = Telnet.Parse(ClientBuffer.InternalBuffer, out byte[] remaining);
                                ClientBuffer = new BufferBuilder();
                                packets.ForEach(packet =>
                                {
                                    InvokeOnTelnet(packet);
                                });
                                if (remaining.Length > 0)
                                {
                                    ClientBuffer.Add(remaining);
                                }
                            }
                        }
                        if (ClientBuffer.CanConsume())
                        {
                            byte[] buf = BufUtil.StripEOL(ClientBuffer.Consume());
                            ClientBuffer = new BufferBuilder();
                            string data = Encoding.UTF8.GetString(buf);
                            InvokeOnMessage(data);
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
            if (opts.Value.ContainsKey(option))
            {
                return opts.Value.TryGetValue(option, out Telnet.TelnetOptionState value) && value == Telnet.TelnetOptionState.Enabled;
            }
            else
            {
                return false;
            }
        }
        public Telnet.TelnetOptionState GetState(byte option)
        {
            using var opts = Options.Lock();
            if (opts.Value.TryGetValue(option, out Telnet.TelnetOptionState state))
            {
                return state;
            }
            else
            {
                opts.Value.Add(option, Telnet.TelnetOptionState.Disabled);
                return Telnet.TelnetOptionState.Disabled;
            }
        }
        public Telnet.TelnetOptionState GetState(byte option, MutLock<Dictionary<byte, Telnet.TelnetOptionState>> opts)
        {
            if (opts.Value.TryGetValue(option, out Telnet.TelnetOptionState state))
            {
                return state;
            }
            else
            {
                opts.Value[option] = Telnet.TelnetOptionState.Disabled;
                return Telnet.TelnetOptionState.Disabled;
            }
        }
        public void SetOption(byte option, Telnet.TelnetOptionState state)
        {
            using var opts = Options.Lock();
            Telnet.TelnetOptionState currentState = GetState(option, opts);
            opts.Value[option] = state;
        }
        public bool RemoveOption(byte option)
        {
            using var opts = Options.Lock();
            if (!opts.Value.ContainsKey(option))
                return false;
            opts.Value.Remove(option);
            return true;
        }
        public bool IsWaitingOn(byte option)
        {
            return GetState(option) == Telnet.TelnetOptionState.Waiting;
        }
        public async Task<bool> Will(byte option)
        {
            Telnet.TelnetOptionState state = GetState(option);
            switch (state)
            {
                case Telnet.TelnetOptionState.Disabled:
                    // Initiating
                    SetOption(option, Telnet.TelnetOptionState.Waiting);
                    await SendTelnet(Telnet.Command.WILL, option);
                    return true;
                case Telnet.TelnetOptionState.Waiting:
                    // Responding
                    SetOption(option, Telnet.TelnetOptionState.Enabled);
                    await SendTelnet(Telnet.Command.WILL, option);
                    return true;
            }
            return false;
        }
        public async Task<bool> Wont(byte option)
        {
            Telnet.TelnetOptionState state = GetState(option);
            switch (state)
            {
                case Telnet.TelnetOptionState.Enabled:
                case Telnet.TelnetOptionState.Disabled:
                    // Initiating
                    SetOption(option, Telnet.TelnetOptionState.Waiting);
                    await SendTelnet(Telnet.Command.WONT, option);
                    return true;
                case Telnet.TelnetOptionState.Waiting:
                    // Responding
                    SetOption(option, Telnet.TelnetOptionState.Disabled);
                    await SendTelnet(Telnet.Command.WONT, option);
                    return true;
            }
            return false;
        }
        public async Task<bool> Do(byte option)
        {
            Telnet.TelnetOptionState state = GetState(option);
            switch (state)
            {
                case Telnet.TelnetOptionState.Disabled:
                    // Initiating
                    SetOption(option, Telnet.TelnetOptionState.Waiting);
                    await SendTelnet(Telnet.Command.DO, option);
                    return true;
                case Telnet.TelnetOptionState.Waiting:
                    // Responding
                    SetOption(option, Telnet.TelnetOptionState.Enabled);
                    await SendTelnet(Telnet.Command.DO, option);
                    return true;
            }
            return false;
        }
        public async Task<bool> Dont(byte option)
        {
            Telnet.TelnetOptionState state = GetState(option);
            switch (state)
            {
                case Telnet.TelnetOptionState.Enabled:
                case Telnet.TelnetOptionState.Disabled:
                    // Initiating
                    SetOption(option, Telnet.TelnetOptionState.Waiting);
                    await SendTelnet(Telnet.Command.DONT, option);
                    return true;
                case Telnet.TelnetOptionState.Waiting:
                    // Responding
                    SetOption(option, Telnet.TelnetOptionState.Disabled);
                    await SendTelnet(Telnet.Command.DONT, option);
                    return true;
            }
            return false;
        }
        public async Task SendTelnet(byte command, byte option)
        {
            try
            {
                byte[] t = Telnet.CreateTelnetData(command, option);
                await Stream.WriteAsync(t, 0, t.Length);
            }
            catch (Exception)
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
            }
            catch (Exception)
            {
                //
            }
        }
        public async Task WriteLine(string data)
        {
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(data + "\n\r");
                await Stream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch (Exception)
            {
                //
            }
        }
        public async Task Write(string data)
        {
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(data);
                await Stream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch (Exception)
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
        public bool InvokeOnTelnetWill(byte option)
        {
            if (OnTelnetWill != null)
                return OnTelnetWill.Invoke(option);
            return true;
        }
        public bool InvokeOnTelnetWont(byte option)
        {
            if (OnTelnetWont != null)
                return OnTelnetWont.Invoke(option);
            return true;
        }
        public bool InvokeOnTelnetDo(byte option)
        {
            if (OnTelnetDo != null)
                return OnTelnetDo.Invoke(option);
            return true;
        }
        public bool InvokeOnTelnetDont(byte option)
        {
            if (OnTelnetDont != null)
                return OnTelnetDont.Invoke(option);
            return true;
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
                        tclient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                        tclient.Client.NoDelay = true;
                        XeonClient xclient = new XeonClient(tclient);
                        Log.WriteLine($"Client <{xclient.GUID}> connected: {xclient.RemoteEndPoint}");
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
                        await xclient.WriteLine("Welcome to the Xeon Project.");
                        await xclient.Write("lua> ");
                        await xclient.Will(Telnet.Option.LineMode);
                        await xclient.Will(Telnet.Option.GMCP);
                        InvokeClientConnect(xclient);
                    }
                }
            });
            NetworkThread.Start();
        }
    }
}
