using System.Collections.Concurrent;

namespace async;

class Program
{
    static void Main(string[] args)
    {
        AsyncLocal<int> asyncLocal = new();
        for (int i = 0; i < 1000; i++)
        {
            asyncLocal.Value = i;
            int index = i; // Create a local variable and assign the value of i to it
            MyThreadPool.QueueUserWorkItem(delegate
            {
                Console.WriteLine(asyncLocal.Value); // Use the local variable instead of i
                Thread.Sleep(1000);
            });
        }

        Console.ReadLine();
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