using Newtonsoft.Json;
using System.IO;
using System.Text;
using XeonCommon;

namespace XeonProject
{
    public struct WebSocketConfig
    {
        public string Address;
        public int Port;
    }
    public struct DataStoreConfig
    {
        public string Path;
    }
    public struct ProgramConfig
    {
        public WebSocketConfig WebSocket;
        public DataStoreConfig DataStore;
    }
    public static class Config
    {
        private static Logger Log = new Logger("[Config]");
        public static readonly ProgramConfig Defaults = new ProgramConfig
        {
            WebSocket = new WebSocketConfig { Address = "127.0.0.1", Port = 1337 },
            DataStore = new DataStoreConfig { Path = "WorldDataStore.json" }
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
            catch
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