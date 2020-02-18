using System.Reflection;
using System.IO;
using XeonCommon;
using XeonCommon.Config;

namespace XeonProject
{
    static class Program
    {
        public static ConcurrentDictionary<string, object> Globals = new ConcurrentDictionary<string, object>();
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

            DataStorage.Setup();
            Network.Start();
            Game.GameThread.Start();
            Events.EventLoop.Start();
            Globals["EventLoop"] = Events.EventLoop;
            Globals["Database"] = DataStorage.Database;
            Globals["AppDir"] = AppDir;
            Globals["Config"] = Config;
            Globals["Args"] = args;
            Globals["NetManager"] = Network.Manager;
            Plugins.LoadPlugins();
            Events.EventLoop.Join();
        }
    }
}
