using Newtonsoft.Json;
using System.IO;
using System.Text;
using XeonCommon;
using System;

namespace XeonProject
{
    struct NetConfig
    {
        internal string address;
        public string Address { get => address; set => address = value; }
        internal int port;
        public int Port { get => port; set => port = value; }
    }
    struct DataStoreConfig
    {
        internal string path;
        public string Path { get => path; set => path = value; }
    }
    struct ProgramConfig
    {
        internal string pluginPath;
        internal NetConfig network;
        internal DataStoreConfig dataStore;
        public string PluginPath { get => pluginPath; set => pluginPath = value; }
        public NetConfig Network { get => network; set => network = value; }
        public DataStoreConfig DataStore { get => dataStore; set => dataStore = value; }
    }
    static class Config
    {
        public static readonly Logger Log = new Logger("[Config]");
        public static readonly ProgramConfig Defaults = new ProgramConfig
        {
            PluginPath = "plugins",
            Network = new NetConfig { Address = "127.0.0.1", Port = 1337 },
            DataStore = new DataStoreConfig { Path = "database" }
        };
        public static ProgramConfig LoadConfig(string configPath)
        {
            try
            {
                using (FileStream fs = File.OpenRead(configPath))
                {
                    Log.WriteLine($"Loading configuration from {configPath}...");
                    byte[] buffer = new byte[fs.Length];
                    fs.Read(buffer, 0, buffer.Length);
                    return JsonConvert.DeserializeObject<ProgramConfig>(Encoding.UTF8.GetString(buffer));
                }
            }
            catch (Exception)
            {
                Log.WriteLine($"Error loading configuration from {configPath}. Loading defaults...");
                using (FileStream fs = File.Create(configPath))
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Defaults, Formatting.Indented));
                    fs.Write(buffer, 0, buffer.Length);
                }
                return Defaults;
            }
        }
    }
}