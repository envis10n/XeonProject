using System;
using System.Reflection;
using System.Threading;
using XeonCore.Sandbox;
using XeonCore.Network;
using XeonCore.Network.Websocket;
using XeonStorage;
using XeonCore.Events;
using XeonCore;
using System.Collections.Generic;
using System.IO;

namespace XeonProject
{
    static class Program
    {
        public static string AppDir = new FileInfo(Assembly.GetEntryAssembly().Location).Directory.ToString();
        public static Cache cache = new Cache(1000 * 60, AppDir, "WorldCache.json");
        public static LuaSandbox sandbox = new LuaSandbox();
        public static NetManager<WClient> Manager = new NetManager<WClient>();
        static void Main(string[] args)
        {
            EventLoop eventLoop = new EventLoop(new KeyValuePair<string, object>("Manager", Manager), new KeyValuePair<string, object>("cache", cache), new KeyValuePair<string, object>("sandbox", sandbox));
            eventLoop.Globals["eventLoop"] = eventLoop;

            Thread NetworkThread = new Thread(() =>
            {
                WServer server = new WServer("127.0.0.1", 1337);
                server.Connect += (WClient client) =>
                {
                    Console.WriteLine($"Client connected: {client.Ip}");
                    client.MessageReceived += (string data) =>
                    {
                        NetEvent<WClient> e = new NetEvent<WClient>();
                        e.Client = client;
                        e.Payload = data;
                        Manager.Queue.CallNetEvent(e);
                    };
                };

                server.Disconnect += (WClient client) =>
                {
                    using (client)
                    {
                        Console.WriteLine($"Client disconnected: {client.Ip}");
                    }
                };
            });
            Thread NetQueueWatcher = new Thread(() =>
            {
                while (true)
                {
                    bool success = Manager.Queue.Poll(out NetEvent<WClient> e);
                    if (success)
                    {
                        Manager.EmitNetEvent(e);
                    }
                }
            });
            Thread GameThread = new Thread(() =>
            {
                Manager.NetEventIn += (NetEvent<WClient> e) =>
                {
                    eventLoop.Enqueue(async () =>
                    {
                        Console.WriteLine($"Network event received from {e.Client.Ip}: {e.Payload}");
                        try
                        {
                            object[] result = await sandbox.Exec(e.Payload, 5000);
                            if (result[0] == null)
                            {
                                // Lua syntax error
                                Console.WriteLine($"Lua syntax error: {result[1]}");
                                await e.Client.Send($"Lua syntax error: {result[1]}");
                            }
                            else if (!(bool)result[0])
                            {
                                // Lua runtime error
                                Console.WriteLine($"Lua runtime error: {result[1]}");
                                await e.Client.Send($"Lua runtime error: {result[1]}");
                            }
                            else
                            {
                                // Success
                                Console.WriteLine("Lua script result:");
                                object[] arr = new object[result.Length - 1];
                                string res = "Script Result: ";
                                Array.Copy(result, 1, arr, 0, arr.Length);
                                foreach (object o in arr)
                                {
                                    Console.WriteLine(o);
                                    if (o != null)
                                        res += "\n" + o.ToString();
                                    else
                                        res += "\nnull";
                                }
                                await e.Client.Send(res);
                            }
                        }
                        catch (SandboxException ex)
                        {
                            Console.WriteLine($"Lua sandbox error: {ex.Message}");
                            await e.Client.Send($"Lua sandbox error: {ex.Message}");
                        }
                        catch (TimeoutException)
                        {
                            Console.WriteLine("Lua script timed out.");
                            await e.Client.Send("Lua script timed out.");
                        }
                    });
                };
            });
            NetworkThread.Start();
            NetQueueWatcher.Start();
            GameThread.Start();
            eventLoop.Start();
            eventLoop.Join();
        }

        static void TestCache()
        {
            Actor t = new Actor();
            Guid tg = Guid.NewGuid();
            cache.TryAdd(tg, t);
            Thread CacheTest = new Thread(() =>
            {
                if (cache.TryGetObject(tg, out CacheObject obj))
                {
                    Actor act = obj.Lock<Actor>();
                    act.Location.X += 5;
                    Console.WriteLine($"Thread 1 Object:\n{act}");
                    obj.Release();
                }
            });
            Thread CacheTest2 = new Thread(() =>
            {
                if (cache.TryGetObject(tg, out CacheObject obj))
                {
                    Actor act = obj.Lock<Actor>();
                    act.Location.X += 100;
                    act.Location.Y += 2;
                    Console.WriteLine($"Thread 2 Object:\n{act}");
                    obj.Release();
                }
            });
            CacheTest.Start();
            CacheTest2.Start();
            while (CacheTest.IsAlive || CacheTest2.IsAlive) { }
            Console.WriteLine($"Snapshot:\n{cache.TakeSnapshot().GetString()}");
        }
    }
}
