using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wireboy.Socket.P2PClient;

namespace P2PServiceHome
{
    class Program
    {
        static void Main(string[] args)
        {
            ConfigServer.LoadFromFile();
            ServiceMenu menu = new ServiceMenu();
            menu.ShowMenu();
        }
    }
}
