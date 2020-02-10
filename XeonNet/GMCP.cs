using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace XeonNet
{
    public static class GMCP
    {
        public class InvalidGmcpPacketException : Exception
        {
            public InvalidGmcpPacketException() { }
            public InvalidGmcpPacketException(string message) : base(message) { }
            public InvalidGmcpPacketException(string message, Exception inner) : base(message, inner) { }
        }
        public struct GmcpData
        {
            public string Namespace;
            public string Path;
            public Dictionary<string, object> Payload;
            public static GmcpData FromTelnetPacket(Telnet.TelnetPacket packet)
            {
                if (packet.Option != Telnet.Option.GMCP || packet.Payload == null)
                    throw new InvalidGmcpPacketException();
                GmcpData temp = new GmcpData();
                string payload = Encoding.UTF8.GetString(packet.Payload);
                int ns = payload.IndexOf('.');
                int nsep = payload.IndexOf(' ');
                string nameSpace = payload.Substring(0, ns);
                string path = payload.Substring(ns + 1, nsep - (ns + 1));
                string data = payload.Substring(nsep + 1, payload.Length - (nsep + 1));
                temp.Namespace = nameSpace;
                temp.Path = path;
                temp.Payload = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
                return temp;
            }
            public override string ToString()
            {
                return $"[XeonNet.GmcpData]\n{JsonConvert.SerializeObject(this, Formatting.Indented)}";
            }
        }
    }
}
