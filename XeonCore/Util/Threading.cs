using System;
using System.Threading;
using System.Threading.Tasks;

namespace XeonCore
{
    public static class Threading
    {
        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout)
        {
            using (var timeoutCancellationTokenSource = new CancellationTokenSource())
            {
                task.Start();
                var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                if (completedTask == task)
                {
                    timeoutCancellationTokenSource.Cancel();
                    return await task;  // Very important in order to propagate exceptions
                }
                else
                {
                    throw new TimeoutException();
                }
            }
        }
    }
    namespace Concurrency
    {
        public class Mut<T>
        {
            protected T _value;
            protected Mutex _mutex = new Mutex();
            public Mut(T value)
            {
                _value = value;
            }
            public ref T Lock()
            {
                _mutex.WaitOne();
                return ref _value;
            }
            public void Release()
            {
                _mutex.ReleaseMutex();
            }
        }
    }
}
