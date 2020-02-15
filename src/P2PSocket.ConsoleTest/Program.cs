using P2PSocket.Core.Models;
using P2PSocket.Core.Extends;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using P2PSocket.Client;
using System.IO;

namespace P2PSocket.ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            MemoryStream ms = new MemoryStream();
            StreamWriter sw = new StreamWriter(ms);
            sw.Write("11111\r\n2222222\r\n333333");
            sw.Flush();
            StreamReader sr = new StreamReader(ms);
            sr.BaseStream.Position = 0;
            string l1 = sr.ReadLine();
            string l2 = sr.ReadLine();
            ms.Close();
        }
        static void Main2(string[] args)
        {
            CoreModule module = new CoreModule();
            module.Start();
            while (true)
            {
                ConsoleKey key = Console.ReadKey().Key;
                if (key == ConsoleKey.R)
                {
                    Global.PortMapList.Clear();
                    Global.PortMapList.Add(new PortMapItem() { LocalPort = 11232, RemoteAddress = "home", RemotePort = 3389 });
                    module.ReloadConfig();
                }
                else if (key == ConsoleKey.Q)
                {
                    break;
                }
            }

        }


    }
}
