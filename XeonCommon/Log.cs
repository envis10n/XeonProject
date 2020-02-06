using System;
using System.Text;

namespace XeonCommon
{
    public class Logger
    {
        public string Prefix;
        public bool Timestamp;
        public Func<DateTime, string> TimeFormat = (DateTime x) => x.ToString();
        public Logger(string prefix, bool timestamp = true)
        {
            Prefix = prefix;
            Timestamp = timestamp;
        }
        public string GetPrefix()
        {
            string t = "";
            if (Timestamp)
            {
                t += $"[{TimeFormat(DateTime.Now)}]";
            }
            return $"{t}{Prefix} ";
        }
        public void Write(params string[] args)
        {
            string t = GetPrefix();
            for (int i = 0; i < args.Length; i++)
            {
                if (i < args.Length - 1)
                {
                    t += $"{args[i]} ";
                }
                else
                {
                    t += args[i];
                }
            }
            Console.Write(t);
        }
        public void WriteLine(params string[] args)
        {
            string t = GetPrefix();
            for (int i = 0; i < args.Length; i++)
            {
                if (i < args.Length - 1)
                {
                    t += $"{args[i]} ";
                }
                else
                {
                    t += args[i];
                }
            }
            Console.WriteLine(t);
        }
    }
}