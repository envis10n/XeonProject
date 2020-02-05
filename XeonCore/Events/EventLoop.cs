using System;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace XeonCore.Events
{
    public class EventGlobals
    {
        Dictionary<string, object> globals = new Dictionary<string, object>();
        public object this[string index]
        {
            get
            {
                if (globals.TryGetValue(index, out object item))
                {
                    return item;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (globals.TryGetValue(index, out object item))
                {
                    item = value;
                }
                else
                {
                    globals.Add(index, value);
                }
            }
        }
    }
    public class EventLoop
    {
        private ConcurrentQueue<Action> Events = new ConcurrentQueue<Action>();
        public Thread EventsThread;
        public readonly EventGlobals Globals = new EventGlobals();
        public bool IsAlive { get => EventsThread.IsAlive; }
        public EventLoop(params KeyValuePair<string, object>[] globals)
        {
            EventsThread = new Thread(StartEventLoop);
            foreach (KeyValuePair<string, object> g in globals)
            {
                Globals[g.Key] = g.Value;
            }
        }
        public void Start()
        {
            EventsThread.Start();
        }
        public void Join()
        {
            EventsThread.Join();
        }
        public void Abort()
        {
            EventsThread.Abort();
        }
        public void Enqueue(Action action)
        {
            Events.Enqueue(action);
        }
        public void StartEventLoop()
        {
            while (true)
            {
                if (Events.Count > 0)
                {
                    if (Events.TryDequeue(out Action action))
                    {
                        action();
                    }
                }
            }
        }
    }
}