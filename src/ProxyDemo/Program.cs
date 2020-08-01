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

            object block = new object();
            new Task(() =>
            {
                Monitor.Wait(block);
            }).Wait();
        }
    }
}
