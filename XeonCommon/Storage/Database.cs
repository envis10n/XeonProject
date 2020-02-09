using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace XeonCommon.Storage
{
    public abstract class DBDocument : Dictionary<string, object>
    {
        protected Mutex Mut = new Mutex();
        public new abstract object this[string index] { get; set; }
        public abstract void Lock();
        public abstract void Release();
    }
    public abstract class DBCollection<T> : List<T> where T : DBDocument
    {
        protected Mutex Mut = new Mutex();
        public abstract T FindOne(Predicate<T> predicate);
        public abstract List<T> FindMatching(Predicate<T> predicate);
        public abstract void Each(Action<T> action);
        public abstract bool RemoveOne(Predicate<T> predicate);
        public abstract int RemoveMatching(Predicate<T> predicate);
        public abstract void Lock();
        public abstract void Release();
    }
    public abstract class DB<T, C> where T : DBDocument where C : DBCollection<T>
    {
        public readonly string Path;
        protected Mutex Mut = new Mutex();
        protected Dictionary<string, C> Collections = new Dictionary<string, C>();
        public abstract bool AddCollection(string name, out C collection);
        public abstract bool RemoveCollection(string name);
        public abstract void Save();
        public abstract bool Load();
        public abstract C GetCollection(string name);
    }
}
