/*
 * 程序入口 
 * 
 */
using System;
using System.Net;
using System.Net.Sockets;
using Wireboy.Socket.P2PService.Services;

namespace Wireboy.Socket.P2PService
{
    class Program
    {
        static void Main(string[] args)
        {
            ConfigServer.LoadFromFile();
            P2PService service = new P2PService();
            service.Start();
        }
    }
}
