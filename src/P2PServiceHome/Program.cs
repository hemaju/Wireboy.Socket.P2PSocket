using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PServiceHome
{
    class Program
    {
        static void Main(string[] args)
        {
            P2PService p2PService = new P2PService();
            p2PService.Start();
        }
    }
}
