using System.Reflection;
using System.IO;
using System;

namespace XeonProject
{
    static class Program
    {
        public static string AppDir = new FileInfo(Assembly.GetEntryAssembly().Location).Directory.ToString();
        public static ProgramConfig Config;
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                string path;
                if (Path.GetPathRoot(args[0]) != null)
                {
                    path = args[0];
                }
                else
                {
                    path = Path.Join(AppDir, args[0]);
                }
                Config = XeonProject.Config.LoadConfig(path);
            }
            else
            {
                Config = XeonProject.Config.LoadConfig(Path.GetFullPath("XeonConfig.json", AppDir));
            }
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
