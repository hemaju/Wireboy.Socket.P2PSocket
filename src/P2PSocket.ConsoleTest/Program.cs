using P2PSocket.Core.Models;
using P2PSocket.Core.Extends;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace P2PSocket.ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {

            new TaskFactory().StartNew(() =>
            {
                Server.CoreModule coreModule = new Server.CoreModule();
                coreModule.Start();
            });
            {
                Client.CoreModule coreModule = new Client.CoreModule();
                coreModule.Start();
            }
            Console.ReadKey();
        }
    }
}
