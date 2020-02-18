namespace XeonCommon.Config
{
    public struct NetConfig
    {
        internal string address;
        public string Address { get => address; set => address = value; }
        internal int port;
        public int Port { get => port; set => port = value; }
    }
    public struct DataStoreConfig
    {
        internal string path;
        public string Path { get => path; set => path = value; }
    }
    public struct ProgramConfig
    {
        internal string pluginPath;
        internal NetConfig network;
        internal DataStoreConfig dataStore;
        public string PluginPath { get => pluginPath; set => pluginPath = value; }
        public NetConfig Network { get => network; set => network = value; }
        public DataStoreConfig DataStore { get => dataStore; set => dataStore = value; }
    }
}