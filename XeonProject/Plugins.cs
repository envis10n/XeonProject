using System;
using System.IO;
using System.Reflection;
using XeonCommon.Threads;
using System.Collections.Generic;
using XeonCommon;

namespace XeonProject
{
    class Plugin
    {
        private Assembly assembly;
        public readonly string FilePath;
        public string Name { get => Path.GetFileNameWithoutExtension(FilePath); }
        private WrapMutex<List<IPlugin>> PluginList = new WrapMutex<List<IPlugin>>(new List<IPlugin>());
        public Plugin(string filePath)
        {
            FilePath = filePath;
            assembly = Assembly.LoadFile(FilePath);
            using var listObj = PluginList.Lock();
            foreach (Type type in assembly.GetTypes())
            {
                if (typeof(IPlugin).IsAssignableFrom(type))
                {
                    IPlugin plugin = (IPlugin)Activator.CreateInstance(type);
                    listObj.Value.Add(plugin);
                    plugin.Init(Program.Globals);
                }
            }
        }
    }
    static class Plugins
    {
        private static Logger Log = new Logger("[Plugins]");
        public static WrapMutex<List<Plugin>> PluginContainer = new WrapMutex<List<Plugin>>(new List<Plugin>());
        public static void LoadPlugins()
        {
            string pluginPath = !Path.IsPathRooted(Program.Config.PluginPath) ?
            Path.GetFullPath(Program.Config.PluginPath,
            Program.AppDir) : Program.Config.PluginPath;
            Log.WriteLine("Loading plugins...");
            if (XeonCommon.IO.Directory.Exists(pluginPath))
            {
                // Begin load
                foreach (string file in XeonCommon.IO.Directory.GetFiles(pluginPath))
                {
                    Plugin plugin = new Plugin(file);
                    Log.WriteLine($"Loaded plugin: {plugin.Name}");
                    using var inList = PluginContainer.Lock();
                    inList.Value.Add(plugin);
                }
            }
            else
            {
                XeonCommon.IO.Directory.Create(pluginPath);
            }
            using var list = PluginContainer.Lock();
            Log.WriteLine($"Loaded {list.Value.Count} plugin(s).");
        }
    }
}