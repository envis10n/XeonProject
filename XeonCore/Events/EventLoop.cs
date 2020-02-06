using System;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json;

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
    public struct EventLoopFrame
    {
        public double Delta;
        public int Count;
    }
    public class EventLoop
    {
        private ConcurrentQueue<Action> Events = new ConcurrentQueue<Action>();
        private Mutex Mut = new Mutex();
        public Thread EventsThread;
        public readonly EventGlobals Globals = new EventGlobals();
        public bool IsAlive { get => EventsThread.IsAlive; }
        public double TickRate { get; }
        public List<EventLoopFrame> Frames = new List<EventLoopFrame>();
        public EventLoop(int tickHz, params KeyValuePair<string, object>[] globals)
        {
            TickRate = (1 / tickHz) * 1000;
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
            Mut.WaitOne();
            Events.Enqueue(action);
            Mut.ReleaseMutex();
        }
        public string GetFrames()
        {
            return JsonConvert.SerializeObject(Frames, Formatting.Indented);
        }
        public void StartEventLoop()
        {
            while (true)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(TickRate));
                Mut.WaitOne();
                if (Events.Count > 0)
                {
                    int count = Events.Count;
                    DateTime start = DateTime.Now;
                    while (Events.TryDequeue(out Action action))
                    {
                        action();
                    }
                    DateTime stop = DateTime.Now;
                    TimeSpan t = stop - start;
                    Frames.Add(new EventLoopFrame { Delta = t.TotalMilliseconds, Count = count });
                    if (Frames.Count > 30)
                    {
                        Frames.RemoveAt(0);
                        Frames.TrimExcess();
                    }
                }
                Mut.ReleaseMutex();
            }
        }
    }
}