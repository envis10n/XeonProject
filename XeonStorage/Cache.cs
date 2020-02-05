using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using XeonCommon.Storage;

namespace XeonStorage
{
    public class CacheSnapshot
    {
        public Dictionary<Guid, CacheObject> Map;
        public DateTime Timestamp;
        public byte[] GetBytes()
        {
            return Encoding.UTF8.GetBytes(GetString());
        }
        public string GetString()
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, ReferenceLoopHandling = ReferenceLoopHandling.Ignore, PreserveReferencesHandling = PreserveReferencesHandling.All });
        }
    }
    [JsonObject(MemberSerialization.OptIn)]
    public class CacheObject : StorageObject
    {
        [JsonProperty]
        private readonly object Value;
        private readonly Mutex Mut = new Mutex();

        public CacheObject(object value)
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
    public class Cache
    {
        private Dictionary<Guid, CacheObject> _map = new Dictionary<Guid, CacheObject>();
        private Mutex _mutex = new Mutex();
        public string Path { get; }
        public int Interval { get; }
        private Timer _saveTimer;
        public Timer SaveTimer { get => _saveTimer; }
        public Cache(int saveInterval, params string[] pathSegments)
        {
            Interval = saveInterval;
            Path = System.IO.Path.Join(pathSegments);
            Load().Wait();
            _saveTimer = new Timer(async (Object stateInfo) =>
            {
                await Save();
            }, null, Interval, Interval);
        }
        public async Task Save()
        {
            CacheSnapshot snapshot = TakeSnapshot();
            await IO.WriteFile(snapshot.GetBytes(), Path);
            Console.WriteLine($"[{snapshot.Timestamp}] World cache saved.");
        }
        private async Task Load()
        {
            Console.WriteLine("Cache loading from disk...");
            try
            {
                byte[] data = await IO.ReadFile(Path);
                LoadSnapshot(data);
                Console.WriteLine($"Cache loaded from snapshot. Bytes: {data.Length}");
            }
            catch (System.IO.FileNotFoundException)
            {
                Console.WriteLine("Cache file not found. Writing new file...");
                await IO.WriteFile(TakeSnapshot().GetBytes(), Path);
            }
            Console.WriteLine("Done.");
        }
        public bool ContainsKey(Guid key)
        {
            return _map.ContainsKey(key);
        }
        public bool ContainsValue(CacheObject item)
        {
            return _map.ContainsValue(item);
        }
        public bool TryGetObject(Guid key, out CacheObject item)
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
            if (TryGetObject(key, out CacheObject item))
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
        public CacheObject GetObject(Guid key)
        {
            if (ContainsKey(key))
            {
                _mutex.WaitOne();
                CacheObject item = _map.GetValueOrDefault(key);
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
                CacheObject obj = new CacheObject(item);
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
        public CacheSnapshot TakeSnapshot()
        {
            _mutex.WaitOne();
            CacheSnapshot snapshot = new CacheSnapshot();
            snapshot.Map = _map;
            snapshot.Timestamp = DateTime.Now;
            _mutex.ReleaseMutex();
            return snapshot;
        }
        public void LoadSnapshot(byte[] data)
        {
            _mutex.WaitOne();
            _map.Clear();
            CacheSnapshot snapshot = JsonConvert.DeserializeObject<CacheSnapshot>(Encoding.UTF8.GetString(data), new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, ReferenceLoopHandling = ReferenceLoopHandling.Ignore, PreserveReferencesHandling = PreserveReferencesHandling.All });
            _map = snapshot.Map;
            _mutex.ReleaseMutex();
        }
        public void LoadSnapshot(string json)
        {
            _mutex.WaitOne();
            _map.Clear();
            CacheSnapshot snapshot = JsonConvert.DeserializeObject<CacheSnapshot>(json, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, ReferenceLoopHandling = ReferenceLoopHandling.Ignore, PreserveReferencesHandling = PreserveReferencesHandling.All });
            _map = snapshot.Map;
            _mutex.ReleaseMutex();
        }
    }
}
