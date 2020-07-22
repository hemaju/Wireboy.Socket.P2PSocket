using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProxyDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            ClientListen client = new ClientListen();

            new Task(() =>
            {
                Monitor.Wait(new object());
            }).Wait();
        }
    }
}
