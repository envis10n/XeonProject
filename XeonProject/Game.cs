using System.Threading;
using XeonCore.Network;
using XeonNet.Sockets;
using System;
using XeonCore.Sandbox;

namespace XeonProject
{
    static class Game
    {
        public static Thread GameThread = new Thread(() =>
            {
                Network.Manager.NetEventIn += (NetEvent<XeonClient> e) =>
                {
                    if (e.IsDisconnect)
                        return;
                    Events.EventLoop.Enqueue(async () =>
                    {
                        Console.WriteLine($"Network event received from {e.Guid}: {e.Payload}");
                        try
                        {
                            object[] result = await Sandbox.Lua.Exec(e.Payload, 5000);
                            if (result[0] == null)
                            {
                                // Lua syntax error
                                Console.WriteLine($"Lua syntax error: {result[1]}");
                                Console.WriteLine($"Payload length: {e.Payload.Length}");
                                await e.Client.WriteLine($"Lua syntax error: {result[1]}");
                                await e.Client.Write("lua> ");
                            }
                            else if (!(bool)result[0])
                            {
                                // Lua runtime error
                                Console.WriteLine($"Lua runtime error: {result[1]}");
                                await e.Client.WriteLine($"Lua runtime error: {result[1]}");
                                await e.Client.Write("lua> ");
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
                                await e.Client.WriteLine(res);
                                await e.Client.Write("lua> ");
                            }
                        }
                        catch (SandboxException ex)
                        {
                            Console.WriteLine($"Lua sandbox error: {ex.Message}");
                            await e.Client.WriteLine($"Lua sandbox error: {ex.Message}");
                            await e.Client.Write("lua> ");
                        }
                        catch (TimeoutException)
                        {
                            Console.WriteLine("Lua script timed out.");
                            await e.Client.WriteLine("Lua script timed out.");
                            await e.Client.Write("lua> ");
                        }
                    });
                };
            });
    }
}