using System;
using System.ServiceProcess;

namespace P2PSocket.StartUp_WinService
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceBase.Run(new P2PSocket());
        }
    }
}
