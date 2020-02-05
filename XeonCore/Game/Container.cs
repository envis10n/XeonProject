using System.Collections.Generic;

namespace XeonCore.Game
{
    public abstract class Container<T> : Entity where T : Entity
    {
        public List<T> Storage = new List<T>();
    }
}
