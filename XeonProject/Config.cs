using Newtonsoft.Json;
using System.IO;
using System.Text;
using XeonCommon;
using XeonCommon.Config;
using System;

namespace XeonProject
{
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