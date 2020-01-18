using P2PSocket.Core.Models;
using P2PSocket.Core.Extends;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace P2PSocket.ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            TcpListener listen = new TcpListener(IPAddress.Any, 11122);
            listen.Start();
            Task.Factory.StartNew(() =>
            {
                try
                {
                    while (true)
                    {
                        listen.AcceptSocket();
                    }
                }
                catch (Exception ex)
                {

                }
            });
            Console.ReadKey();
            listen.Stop();
            Console.ReadKey();
        }
    }
}
