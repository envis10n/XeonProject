using System;

namespace XeonCommon
{
    public interface IPlugin
    {
        public void Init(ConcurrentDictionary<string, object> globals);
    }
}