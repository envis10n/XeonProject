using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace XeonNet
{
    public static class GMCP
    {
        public class JsonObject
        {
            public readonly bool IsArray;
            public readonly object Value;
            public JsonObject(bool isArray, object value)
            {
                IsArray = isArray;
                Value = value;
            }
        }
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
            public JsonObject Payload;
            public static GmcpData FromTelnetPacket(Telnet.TelnetPacket packet)
            {
                if (packet.Option != Telnet.Option.GMCP || packet.Payload == null)
                    throw new InvalidGmcpPacketException();
                GmcpData temp = new GmcpData();
                string payload = Encoding.UTF8.GetString(packet.Payload);
                Console.WriteLine($"Debug GMCP Payload: {payload}");
                int ns = payload.IndexOf('.');
                int nsep = payload.IndexOf(' ');
                string nameSpace;
                string path;
                string data;
                if (nsep == -1)
                {
                    nameSpace = payload.Substring(0, ns);
                    path = payload.Substring(ns + 1, payload.Length - (ns + 1));
                }
                else
                {
                    nameSpace = payload.Substring(0, ns);
                    path = payload.Substring(ns + 1, nsep - (ns + 1));
                    data = payload.Substring(nsep + 1, payload.Length - (nsep + 1));
                    try
                    {
                        JContainer tempPayload = JsonConvert.DeserializeObject<JContainer>(data);
                        switch (tempPayload.Type)
                        {
                            case JTokenType.Object:
                                temp.Payload = new JsonObject(false, tempPayload.ToObject<Dictionary<string, string>>());
                                break;
                            case JTokenType.Array:
                                temp.Payload = new JsonObject(true, tempPayload.ToObject<List<string>>());
                                break;
                        }
                    }
                    catch (Exception)
                    {
                        Console.WriteLine($"Unhandled JSON GMCP Payload: {data}");
                    }
                }
                temp.Namespace = nameSpace;
                temp.Path = path;
                return temp;
            }
            public override string ToString()
            {
                return $"[XeonNet.GmcpData]\n{JsonConvert.SerializeObject(this, Formatting.Indented)}";
            }
        }
    }
}
