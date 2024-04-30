using System.Collections.Concurrent;
using System.Configuration.Assemblies;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic;

namespace async;

class Program
{
    static void Main(string[] args)
    {
        AsyncLocal<int> asyncLocal = new();

        List<MyTask> tasks = new();

        for (int i = 0; i < 1000; i++)
        {
            asyncLocal.Value = i;
            int index = i; // Create a local variable and assign the value of i to it
            tasks.Add(MyTask.Run(delegate
            {
                Console.WriteLine(asyncLocal.Value); // Use the local variable instead of i
                Thread.Sleep(100);
            }));
        }

        Console.ReadLine();
    }

    class MyTask
    {
        private bool _completed;
        private Exception? _exception;
        private Action? _continuation;
        private ExecutionContext? _context;

        public bool IsCompleted
        {
            get
            {
                lock (this)
                {
                    return _completed;
                }
            }
        }
        public void SetResult() => Complete(null);
        public void SetException(Exception e) => Complete(e);

        public void Complete(Exception? exception)
        {
            lock (this)
            {
                if (_completed)
                    throw new InvalidOperationException("The task has already been completed.");

                _completed = true;
                _exception = exception;

                if (_continuation is not null)
                    MyThreadPool.QueueUserWorkItem(delegate
                    {
                        if (_context is null)
                            _continuation();
                        else
                            ExecutionContext.Run(_context, state => ((Action)state!).Invoke(), _continuation);
                    });
            }

        }

        public void Wait()
        {
            ManualResetEventSlim? mres = null;
            lock (this)
            {
                if (!_completed)
                {
                    mres = new ManualResetEventSlim();
                    ContinueWith(mres.Set);
                }
            }

            mres?.Wait();

            if (_exception is not null)
                ExceptionDispatchInfo.Throw(_exception);
            //throw new AggregateException(_exception);
        }

        public void ContinueWith(Action action)
        {
            lock (this)
            {
                if (_completed)
                {
                    MyThreadPool.QueueUserWorkItem(action);
                }
                else
                {
                    _continuation = action;
                    _context = ExecutionContext.Capture();
                }
            }
        }

        public static MyTask Run(Action action)
        {
            MyTask t = new();
            MyThreadPool.QueueUserWorkItem(() =>
            {
                try
                {
                    action();

                }
                catch (Exception e)
                {
                    t.SetException(e);
                    return;
                }
                t.SetResult();
            });

            return t;
        }
    }

    static class MyThreadPool
    {
        public static readonly BlockingCollection<(Action, ExecutionContext?)> s_workItems = new();
        public static void QueueUserWorkItem(Action action) => s_workItems.Add((action, ExecutionContext.Capture()));

        static MyThreadPool()
        {
            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                new Thread(() =>
                {
                    while (true)
                    {
                        (Action workItem, ExecutionContext? context) = s_workItems.Take();
                        if (context is null)
                            workItem();
                        else
                            ExecutionContext.Run(context, state => ((Action)state!).Invoke(), workItem);

                    }
                })
                { IsBackground = true }.Start();
            }
        }

    }
}