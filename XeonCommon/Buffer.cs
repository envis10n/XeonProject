﻿using System;
using System.Collections.Generic;
using System.Text;

namespace XeonCommon
{
    public static class BufUtil
    {
        public enum EOLFormat
        {
            CR,
            LF,
            CRLF
        }
        public static bool HasEOL(byte[] buffer, EOLFormat format = EOLFormat.LF)
        {
            int end = buffer.Length - 1;
            switch (format)
            {
                case EOLFormat.CR:
                    return buffer[end] == 13;
                case EOLFormat.LF:
                    return buffer[end] == 10;
                case EOLFormat.CRLF:
                    return buffer[end - 1] == 13 && buffer[end] == 10;
                default:
                    return buffer[end] == 10;
            }
        }
        public static byte[] StripEOL(byte[] buffer, EOLFormat format = EOLFormat.LF)
        {
            int end = buffer.Length - 1;
            byte[] temp;
            switch (format)
            {
                case EOLFormat.CRLF:
                    temp = new byte[buffer.Length - 2];
                    Buffer.BlockCopy(buffer, 0, temp, 0, temp.Length);
                    return temp;
                default:
                    temp = new byte[buffer.Length - 1];
                    Buffer.BlockCopy(buffer, 0, temp, 0, temp.Length);
                    return temp;
            }
        }
        public static int CountDelim(byte[] data, byte delim)
        {
            int count = 0;
            foreach (byte b in data)
            {
                if (b == delim)
                    count++;
            }
            return count;
        }
        public static int GetNext(byte[] data, int offset, byte delim)
        {
            bool success = false;
            int i;
            for(i = offset;  i < data.Length; i++)
            {
                if (data[i] == delim)
                {
                    success = true;
                    break;
                }
            }
            return success ? i : -1;
        }
        public static List<byte[]> SplitIAC(byte[] data, byte delim, out byte[] remaining)
        {
            List<byte[]> list = new List<byte[]>();
            int i = GetNext(data, 0, delim);
            int iN = i + 1;
            remaining = new byte[0];
            while(iN != -1)
            {
                iN = GetNext(data, i + 1, delim);
                if (iN != -1)
                {
                    // Another IAC after this one.
                    byte[] t = new byte[iN - i];
                    Buffer.BlockCopy(data, i, t, 0, t.Length);
                    list.Add(t);
                    i = iN;
                } else
                {
                    if (data[i + 1] == 240 && i + 1 != data.Length - 1)
                    {
                        // Data left over
                        list.Add(new byte[] { 255, 240 });
                        int len = data.Length - (i + 2);
                        byte[] t = new byte[len];
                        Buffer.BlockCopy(data, i + 2, t, 0, len);
                        remaining = t;
                    }
                    else
                    {
                        int len = data.Length - i;
                        byte[] t = new byte[len];
                        Buffer.BlockCopy(data, i, t, 0, len);
                        list.Add(t);
                    }
                }
            }
            return list;
        }
    }
}
