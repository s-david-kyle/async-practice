using System.Collections.Concurrent;

namespace async;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        for (int i = 0; i < 1000; i++)
        {
            int index = i; // Create a local variable and assign the value of i to it
            MyThreadPool.QueueUserWorkItem(() =>
            {
                Console.WriteLine("Hello, World! " + index); // Use the local variable instead of i
                Thread.Sleep(10);
            });
        }

        Console.ReadLine();
    }
}

static class MyThreadPool
{
    public static readonly BlockingCollection<Action> s_workItems = new();
    public static void QueueUserWorkItem(Action action) => s_workItems.Add(action);



    static MyThreadPool()
    {
        for (int i = 0; i < Environment.ProcessorCount; i++)
        {
            new Thread(() =>
            {
                while (true)
                {
                    Action action = s_workItems.Take();
                    Console.WriteLine(s_workItems.Count);
                    action();
                }
            })
            { IsBackground = true }.Start();
        }
    }

}