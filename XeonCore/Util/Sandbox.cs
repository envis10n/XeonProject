using System;
using NLua;
using XeonCommon.Threads;
using System.Collections.Generic;
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
            protected WrapMutex<string> RunnerSource;
            protected WrapMutex<List<string>> _InternalFunctions = new WrapMutex<List<string>>(new List<string>(new string[] { "print" }));
            public LuaSandbox()
            {
                State = new Lua();
                State.State.Encoding = Encoding.UTF8;
                RunnerSource = new WrapMutex<string>(@"
                import = function () end -- Block import
                local env = {print = print}
                local function run(untrusted_code)
                    local untrusted_function, message = load(untrusted_code, nil, 't', env)
                    if not untrusted_function then return nil, message end
                    return pcall(untrusted_function)
                end
                return run");
            }
            private void UpdateSandboxEnv()
            {
                using var list = _InternalFunctions.Lock();
                using var runner = RunnerSource.Lock();
                string[] listArr = list.Value.ToArray();
                string source = "";
                for (int i = 0; i < listArr.Length; i++)
                {
                    string v = listArr[i];
                    source += $"{v} = {v}";
                    if (i != listArr.Length - 1)
                    {
                        source += ", ";
                    }
                }
                string functionSource = @"
                import = function () end -- Block import
                " + $"local env = {{{source}}}" + @"
                local function run(untrusted_code)
                    local untrusted_function, message = load(untrusted_code, nil, 't', env)
                    if not untrusted_function then return nil, message end
                    return pcall(untrusted_function)
                end
                return run";
                runner.Value = functionSource;
            }
            public bool RegisterFunction(string path, Delegate func)
            {
                bool success = false;
                using (var list = _InternalFunctions.Lock())
                {
                    if (!list.Value.Contains(path))
                    {
                        State[path] = func;
                        list.Value.Add(path);
                        success = true;
                    }
                }
                if (success)
                {
                    UpdateSandboxEnv();
                }
                return success;
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
                        using var runnerSource = RunnerSource.Lock();
                        LuaFunction runner = State.DoString(runnerSource.Value)[0] as LuaFunction;
                        var res = runner.Call(source);
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
