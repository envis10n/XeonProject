using System;
using System.Collections.Generic;

namespace XeonCommon
{
    public class ConcurrentDictionary<T, K>
    {
        Threads.WrapMutex<Dictionary<T, K>> Inner = new Threads.WrapMutex<Dictionary<T, K>>(new Dictionary<T, K>());
        public K this[T index]
        {
            get
            {
                using var obj = Inner.Lock();
                return obj.Value[index];
            }
            set
            {
                using var obj = Inner.Lock();
                obj.Value[index] = value;
            }
        }
    }
}