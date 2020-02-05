using System;
using XeonCore.Game;
using XeonCommon.Storage;

namespace XeonCore.Users
{
    public abstract class Controller<T> where T : Actor
    {
        public Guid GUID { get; }
        public abstract StorageObject GetControlled();
    }
}