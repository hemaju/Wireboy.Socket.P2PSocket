/*
 * 程序入口 
 * 
 */
using System;
using System.Net;
using System.Net.Sockets;

namespace Wireboy.Socket.P2PService
{
    class Program
    {
        static void Main(string[] args)
        {
            P2PService service = new P2PService();
            service.Start();
        }
    }
}
