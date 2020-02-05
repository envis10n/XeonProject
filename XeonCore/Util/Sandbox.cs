using System;
using NLua;
using System.Threading.Tasks;
using System.Text;

namespace XeonCore
{
    namespace Sandbox
    {
        public class SandboxException : Exception
        {
            public SandboxException(string message) : base(message)
            { }
        }
        public class LuaSandbox
        {
            protected Lua State;
            protected LuaFunction Runner;
            public LuaSandbox()
            {
                State = new Lua();
                State.State.Encoding = Encoding.UTF8;
                Runner = State.DoString(@"
                import = function () end -- Block import
                local env = {}
                local function run(untrusted_code)
                    local untrusted_function, message = load(untrusted_code, nil, 't', env)
                    if not untrusted_function then return nil, message end
                    return pcall(untrusted_function)
                end
                return run")[0] as LuaFunction;
            }
            public void UpdateSandboxEnv(string source)
            {
                Runner = State.DoString(@"
                import = function () end -- Block import
                " + $"local env = {source}" + @"
                local function run(untrusted_code)
                    local untrusted_function, message = load(untrusted_code, nil, 't', env)
                    if not untrusted_function then return nil, message end
                    return pcall(untrusted_function)
                end
                return run")[0] as LuaFunction;
            }
            public void RegisterFunction(string path, Delegate func)
            {
                State[path] = func;
            }
            public LuaSandbox(KeraLua.Lua state)
            {
                State = new Lua(state);
            }
            public async Task<object[]> Exec(string source, int timeout)
            {
                Task<object[]> task = new Task<object[]>(() =>
                {
                    try
                    {
                        var res = Runner.Call(source);
                        if (res != null)
                        {
                            return res;
                        }
                        else
                        {
                            return new object[0];
                        }
                    }
                    catch (NLua.Exceptions.LuaScriptException e)
                    {
                        throw new SandboxException(e.Message);
                    }
                }).TimeoutAfter(TimeSpan.FromMilliseconds(timeout));
                return await task;
            }
        }
    }
}
