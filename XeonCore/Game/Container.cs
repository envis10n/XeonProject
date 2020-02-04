using System;
using System.Collections.Generic;

namespace XeonCore
{
    public class Container<T> : Entity where T : Entity
    {
        public List<T> Storage;
        public Container()
        {
            Storage = new List<T>();
        }
    }
}
