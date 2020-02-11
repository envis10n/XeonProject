using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using XeonCommon;

namespace XeonNet
{
    public static class Telnet
    {
        public const byte IAC = 255;
        public static class Command
        {
            public const byte SE = 240;
            public const byte NOP = 241;
            public const byte DataMark = 242;
            public const byte Break = 243;
            public const byte Interrupt = 244;
            public const byte AbortOutput = 245;
            public const byte AreYouThere = 246;
            public const byte EraseCharacter = 247;
            public const byte EraseLine = 248;
            public const byte GoAhead = 249;
            public const byte SB = 250;
            public const byte WILL = 251;
            public const byte WONT = 252;
            public const byte DO = 253;
            public const byte DONT = 254;
        }
        public static class Option
        {
            public const byte Extended = 255;
            public const byte BinaryTransmission = 0;
            public const byte Echo = 1;
            public const byte SuppressGoAhead = 3;
            public const byte Status = 5;
            public const byte TimingMark = 6;
            public const byte GMCP = 201;
            public const byte LineMode = 34;
        }
        public struct TelnetPacket
        {
            public byte Command;
            public byte Option;
            public byte[] Payload;
            public override string ToString()
            {
                if (Payload == null)
                    Payload = new byte[0];
                return $"[XeonNet.Telnet.TelnetPacket]\nCommand: {Command}\nOption: {Option}\nPayload: {Encoding.UTF8.GetString(Payload)}";
            }
        }
        
        public static List<TelnetPacket> Parse(byte[] data, out byte[] remaining)
        {
            List<TelnetPacket> list = new List<TelnetPacket>();
            if (BufUtil.CountDelim(data, IAC) > 0)
            {
                List<byte[]> seq = BufUtil.SplitIAC(data, IAC, out byte[] unparsed);
                remaining = unparsed;
                seq.ForEach(s =>
                {
                    switch (s[1])
                    {
                        case Command.DO:
                        case Command.DONT:
                        case Command.WILL:
                        case Command.WONT:
                            list.Add(new TelnetPacket { Command = s[1], Option = s[2] });
                            break;
                        case Command.SB:
                            byte[] payload = new byte[s.Length - 3];
                            Buffer.BlockCopy(s, 3, payload, 0, payload.Length);
                            list.Add(new TelnetPacket { Command = s[1], Option = s[2], Payload = payload });
                            break;
                        case Command.SE:
                            // Ignore sub-negotiation end.
                            break;
                    }
                });
            } else
            {
                remaining = data;
            }
            return list;
        }
        public static byte[] CreateTelnetData(byte command, byte option)
        {
            return new byte[] { IAC, command, option };
        }
        public static byte[] CreateGMCPData(string path, Dictionary<string, object> payload)
        {
            string p = JsonConvert.SerializeObject(payload);
            byte[] pD = Encoding.UTF8.GetBytes($"{path} {p}");
            byte[] final = new byte[pD.Length + 5];
            final[0] = IAC;
            final[1] = Command.SB;
            final[2] = Option.GMCP;
            Buffer.BlockCopy(pD, 0, final, 3, pD.Length);
            Buffer.BlockCopy(new byte[] { IAC, (byte)Command.SE }, 0, final, pD.Length, 2);
            return final;
        }
    }
}
