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
        public enum Command : byte
        {
            SE = 240,
            NOP = 241,
            DataMark = 242,
            Break = 243,
            Interrupt = 244,
            AbortOutput = 245,
            AreYouThere = 246,
            EraseCharacter = 247,
            EraseLine = 248,
            GoAhead = 249,
            SB = 250,
            WILL = 251,
            WONT = 252,
            DO = 253,
            DONT = 254
        }
        public enum Option : byte
        {
            Extended = 255,
            BinaryTransmission = 0,
            Echo = 1,
            SuppressGoAhead = 3,
            Status = 5,
            TimingMark = 6,
            GMCP = 201
        }
        public struct TelnetPacket
        {
            public Command Command;
            public Option Option;
            public byte[] Payload;
            public override string ToString()
            {
                if (Payload == null)
                    Payload = new byte[0];
                return $"[XeonNet.Telnet.TelnetPacket]\nCommand: {Enum.GetName(Command.GetType(), Command)}\nOption: {Enum.GetName(Option.GetType(), Option)}\nPayload: {Encoding.UTF8.GetString(Payload)}";
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
                    switch ((Command)s[1])
                    {
                        case Command.DO:
                        case Command.DONT:
                        case Command.WILL:
                        case Command.WONT:
                            list.Add(new TelnetPacket { Command = (Command)s[1], Option = (Option)s[2] });
                            break;
                        case Command.SB:
                            byte[] payload = new byte[s.Length - 3];
                            Buffer.BlockCopy(s, 3, payload, 0, payload.Length);
                            list.Add(new TelnetPacket { Command = (Command)s[1], Option = (Option)s[2], Payload = payload });
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
    }
}
