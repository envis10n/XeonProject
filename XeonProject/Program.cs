using System.Reflection;
using System.IO;
using System;

namespace XeonProject
{
    static class Program
    {
        public static string AppDir = new FileInfo(Assembly.GetEntryAssembly().Location).Directory.ToString();
        static void Main(string[] args)
        {
            Sandbox.Lua.RegisterFunction("_GetFrames", new Func<string>(() =>
            {
                return Events.EventLoop.GetFrames();
            }));
            Sandbox.Lua.UpdateSandboxEnv("{ GetFrames = _GetFrames }");
            Network.Start();
            Game.GameThread.Start();
            Events.EventLoop.Start();
            Events.EventLoop.Join();
        }
    }
}
