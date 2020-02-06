using System.Collections.Generic;
using XeonCore.Events;

namespace XeonProject
{
    public static class Events
    {
        public static EventLoop EventLoop = new EventLoop(
            60,
            new KeyValuePair<string, object>("Manager", Network.Manager),
            new KeyValuePair<string, object>("Cache", Storage.Cache),
            new KeyValuePair<string, object>("Lua", Sandbox.Lua)
        );
    }
}