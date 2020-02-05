using System;

namespace XeonCore.Util
{
    public static class Array
    {
        public static bool Some<T>(T[] arr, Func<T, bool> comparator)
        {
            bool res = false;
            foreach (T val in arr)
            {
                res = comparator.Invoke(val);
                if (res)
                    break;
            }
            return res;
        }
    }
}