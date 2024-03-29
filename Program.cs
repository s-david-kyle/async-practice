namespace async;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        for (int i = 0; i < 1000; i++)
        {
            int index = i; // Create a local variable and assign the value of i to it
            ThreadPool.QueueUserWorkItem(delegate
            {
                Console.WriteLine("Hello, World! " + index); // Use the local variable instead of i
                Thread.Sleep(10);
            });
        }
        Console.ReadLine();
    }
}
