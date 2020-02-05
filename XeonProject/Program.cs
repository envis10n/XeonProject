using System.Reflection;
using System.IO;

namespace XeonProject
{
    static class Program
    {
        public static string AppDir = new FileInfo(Assembly.GetEntryAssembly().Location).Directory.ToString();
        static void Main(string[] args)
        {
            Network.Start();
            Game.GameThread.Start();
            Events.EventLoop.Start();
            Events.EventLoop.Join();
        }
    }
}
