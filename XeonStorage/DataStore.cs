using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using XeonCommon.IO;
using XeonCommon.Storage;
using XeonCommon;

namespace XeonStorage
{
    public static class Defaults
    {
        public static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, ReferenceLoopHandling = ReferenceLoopHandling.Ignore, PreserveReferencesHandling = PreserveReferencesHandling.All };
    }
    public class DataStoreSnapshot
    {
        public Dictionary<Guid, DataStoreObject> Map;
        public DateTime Timestamp;
        public byte[] GetBytes()
        {
            return Encoding.UTF8.GetBytes(GetString());
        }
        public string GetString()
        {
            return JsonConvert.SerializeObject(this, Defaults.SerializerSettings);
        }
    }
    [JsonObject(MemberSerialization.OptIn)]
    public class DataStoreObject : StorageObject
    {
        [JsonProperty]
        private readonly object Value;
        private readonly Mutex Mut = new Mutex();

        public DataStoreObject(object value)
        {
            Value = value;
        }
        public override object Lock()
        {
            Mut.WaitOne();
            return Value;
        }
        public override T Lock<T>()
        {
            Mut.WaitOne();
            return (T)Value;
        }
        public override void Release()
        {
            Mut.ReleaseMutex();
        }
        public override bool IsType<C>()
        {
            return Value is C;
        }
    }
    public class DataStore
    {
        private static string AppDir = Environment.CurrentDirectory;
        private static Logger Log = new Logger("[DataStore]");
        private Dictionary<Guid, DataStoreObject> _map = new Dictionary<Guid, DataStoreObject>();
        private Mutex _mutex = new Mutex();
        public string Path { get; }
        public int Interval { get; }
        private Timer _saveTimer;
        public Timer SaveTimer { get => _saveTimer; }
        public DataStore(int saveInterval, params string[] pathSegments)
        {
            Interval = saveInterval;
            Path = System.IO.Path.Combine(pathSegments);
            Load().Wait();
            _saveTimer = new Timer(async (Object stateInfo) =>
            {
                await Save();
            }, null, Interval, Interval);
        }
        public async Task Save()
        {
            DataStoreSnapshot snapshot = TakeSnapshot();
            await File.Async.OverwriteFile(snapshot.GetBytes(), Path);
            Log.WriteLine($"[{snapshot.Timestamp}] World cache saved.");
        }
        private async Task Load()
        {
            Log.WriteLine("DataStore loading from disk...");
            try
            {
                byte[] data = await File.Async.ReadFile(Path);
                LoadSnapshot(data);
                Log.WriteLine($"DataStore loaded from snapshot. Bytes: {data.Length}");
            }
            catch (System.IO.FileNotFoundException)
            {
                Log.WriteLine($"DataStore file not found. Writing new file at {Path}");
                await File.Async.OverwriteFile(TakeSnapshot().GetBytes(), Path);
            }
            Log.WriteLine("Done.");
        }
        public bool ContainsKey(Guid key)
        {
            return _map.ContainsKey(key);
        }
        public bool ContainsValue(DataStoreObject item)
        {
            return _map.ContainsValue(item);
        }
        public bool TryGetObject(Guid key, out DataStoreObject item)
        {
            if (ContainsKey(key))
            {
                _mutex.WaitOne();
                item = _map.GetValueOrDefault(key);
                _mutex.ReleaseMutex();
                return true;
            }
            else
            {
                item = null;
                return false;
            }
        }
        public bool Remove(Guid key)
        {
            if (TryGetObject(key, out DataStoreObject item))
            {
                object obj = item.Lock();
                _map.Remove(key);
                item.Release();
                item = null;
                obj = null;
                return true;
            }
            else
            {
                return false;
            }
        }
        public DataStoreObject GetObject(Guid key)
        {
            if (ContainsKey(key))
            {
                _mutex.WaitOne();
                DataStoreObject item = _map.GetValueOrDefault(key);
                _mutex.ReleaseMutex();
                return item;
            }
            else
            {
                throw new KeyNotFoundException($"{key} not found");
            }
        }
        public bool TryAdd(Guid key, object item)
        {
            _mutex.WaitOne();
            if (!ContainsKey(key))
            {
                DataStoreObject obj = new DataStoreObject(item);
                _map.Add(key, obj);
                _mutex.ReleaseMutex();
                return true;
            }
            else
            {
                _mutex.ReleaseMutex();
                return false;
            }
        }
        public DataStoreSnapshot TakeSnapshot()
        {
            _mutex.WaitOne();
            DataStoreSnapshot snapshot = new DataStoreSnapshot();
            snapshot.Map = JsonConvert.DeserializeObject<Dictionary<Guid, DataStoreObject>>(JsonConvert.SerializeObject(_map, Defaults.SerializerSettings));
            snapshot.Timestamp = DateTime.Now;
            _mutex.ReleaseMutex();
            return snapshot;
        }
        public void LoadSnapshot(byte[] data)
        {
            _mutex.WaitOne();
            _map.Clear();
            DataStoreSnapshot snapshot = JsonConvert.DeserializeObject<DataStoreSnapshot>(Encoding.UTF8.GetString(data), Defaults.SerializerSettings);
            _map = snapshot.Map;
            _mutex.ReleaseMutex();
        }
        public void LoadSnapshot(string json)
        {
            _mutex.WaitOne();
            _map.Clear();
            DataStoreSnapshot snapshot = JsonConvert.DeserializeObject<DataStoreSnapshot>(json, Defaults.SerializerSettings);
            _map = snapshot.Map;
            _mutex.ReleaseMutex();
        }
    }
}
