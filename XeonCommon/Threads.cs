using System;
using System.Threading;

namespace XeonCommon.Threads
{
    public class MutLock<TValue> : IDisposable
    {
        private Mutex Mut;
        public TValue Value;
        public MutLock(TValue val, Mutex mut)
        {
            Value = val;
            Mut = mut;
        }
        public void Dispose()
        {
            Mut.ReleaseMutex();
        }
    }
    public class WrapMutex<TValue> : IDisposable
    {
        private Mutex Mut = new Mutex();
        private TValue Value;
        public WrapMutex(TValue val)
        {
            Value = val;
        }
        public WrapMutex()
        {
            Value = default;
        }
        public void Dispose()
        {
            Mut.Dispose();
        }
        public MutLock<TValue> Lock()
        {
            Mut.WaitOne();
            return new MutLock<TValue>(Value, Mut);
        }
    }
}
