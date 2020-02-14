using System;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using XeonCommon.Storage;
using Newtonsoft.Json;
using XeonCommon.IO;
using XeonCommon;

namespace XeonStorage
{
    [JsonDictionary]
    public class Document : DBDocument
    {
        public override object this[string index]
        {
            get
            {
                Mut.WaitOne();
                bool success = TryGetValue(index, out object v);
                Mut.ReleaseMutex();
                if (success)
                {
                    return v;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                Mut.WaitOne();
                if (ContainsKey(index))
                {
                    if (Remove(index))
                    {
                        Add(index, value);
                    }
                }
                else
                {
                    Add(index, value);
                }
                Mut.ReleaseMutex();
            }
        }
        public static Document FromDictionary(Dictionary<string, object> dictionary)
        {
            return JsonConvert.DeserializeObject<Document>(
                    JsonConvert.SerializeObject(dictionary, Database.SerializerSettings),
                    Database.SerializerSettings);
        }
        public override void Lock()
        {
            Mut.WaitOne();
        }
        public override void Release()
        {
            Mut.ReleaseMutex();
        }
        public override string ToString()
        {
            return $"[XeonStorage.Document]\n{JsonConvert.SerializeObject(this, Formatting.Indented)}";
        }
    }
    [JsonArray]
    public class Collection : DBCollection<Document>
    {
        public void Insert(Document doc)
        {
            Add(doc);
        }
        public void Insert(Dictionary<string, object> dict)
        {
            Add(Document.FromDictionary(dict));
        }
        public override Document FindOne(Predicate<Document> predicate)
        {
            Mut.WaitOne();
            Document t = Find(predicate);
            Mut.ReleaseMutex();
            return t;
        }
        public override List<Document> FindMatching(Predicate<Document> predicate)
        {
            Mut.WaitOne();
            List<Document> l = FindAll(predicate);
            Mut.ReleaseMutex();
            return l;
        }
        public override void Each(Action<Document> action)
        {
            Mut.WaitOne();
            ForEach(action);
            Mut.ReleaseMutex();
        }
        public override bool RemoveOne(Predicate<Document> predicate)
        {
            Mut.WaitOne();
            bool success = Remove(Find(predicate));
            Mut.ReleaseMutex();
            return success;
        }
        public override int RemoveMatching(Predicate<Document> predicate)
        {
            Mut.WaitOne();
            int count = RemoveAll(predicate);
            Mut.ReleaseMutex();
            return count;
        }
        public override void Lock()
        {
            Mut.WaitOne();
        }
        public override void Release()
        {
            Mut.ReleaseMutex();
        }
    }
    public class Database : DB<Document, Collection>    
    {
        public static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings {
            TypeNameHandling = TypeNameHandling.Auto,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            PreserveReferencesHandling = PreserveReferencesHandling.All
        };
        private static Logger Log = new Logger("[Database]");
        private Timer _saveTimer;
        public readonly int Interval;
        public new readonly string Path;
        public Database(string path, int saveInterval)
        {
            Path = path;
            Log.WriteLine($"Database path: {Path}");
            Interval = saveInterval;
            _saveTimer = new Timer((Object stateInfo) =>
            {
                Save();
            }, null, Interval, Interval);
            Load();
        }
        public override bool AddCollection(string name, out Collection collection)
        {
            Mut.WaitOne();
            Collection t = new Collection();
            bool success = Collections.TryAdd(name, t);
            if (success)
            {
                collection = t;
            } else
            {
                collection = null;
            }
            Mut.ReleaseMutex();
            return success;
        }
        public override bool RemoveCollection(string name)
        {
            Mut.WaitOne();
            bool success = Collections.Remove(name);
            Mut.ReleaseMutex();
            return success;
        }
        public override void Save()
        {
            Log.WriteLine("Saving database...");
            Mut.WaitOne();
            Dictionary<string, Collection>.Enumerator en = Collections.GetEnumerator();
            while (en.MoveNext())
            {
                string path = System.IO.Path.Join(Path, $"{en.Current.Key}.json");
                en.Current.Value.Lock();
                byte[] data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(en.Current.Value, Database.SerializerSettings));
                File.Sync.OverwriteFile(data, path);
                en.Current.Value.Release();
            }
            Mut.ReleaseMutex();
            Log.WriteLine("Done.");
        }
        public override Collection GetCollection(string name)
        {
            Mut.WaitOne();
            bool success = Collections.TryGetValue(name, out Collection val);
            Mut.ReleaseMutex();
            if (!success)
                return null;
            else
                return val;
        }
        public bool HasCollection(string name)
        {
            Mut.WaitOne();
            bool success = Collections.TryGetValue(name, out Collection val);
            Mut.ReleaseMutex();
            return success;
        }
        public override bool Load()
        {
            Log.WriteLine($"Loading database...");
            if (Directory.Exists(Path))
            {
                Mut.WaitOne();
                Collections.Clear();
                string[] files = Directory.GetFiles(Path);
                foreach (string file in files)
                {
                    // file = Full Path
                    string name = System.IO.Path.GetFileNameWithoutExtension(file);
                    Collection c = JsonConvert.DeserializeObject<Collection>(
                        Encoding.UTF8.GetString(File.Sync.ReadFile(file)),
                        SerializerSettings);
                    Collections.Add(name, c);
                }
                Mut.ReleaseMutex();
                Log.WriteLine($"Loaded {files.Length} collections.");
                return true;
            } else
            {
                Mut.WaitOne();
                Directory.Create(Path);
                Collections.Clear();
                Mut.ReleaseMutex();
                Log.WriteLine("No database found. Created directory.");
                return false;
            }
        }
    }
}
