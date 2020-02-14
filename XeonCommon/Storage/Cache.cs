using System.Threading;

namespace XeonCommon.Storage
{
    public abstract class StorageObject
    {
        private readonly object Value;
        private Mutex Mut = new Mutex();
        public abstract object Lock();
        public abstract T Lock<T>();
        public abstract void Release();
        public abstract bool IsType<C>();
    }
}
