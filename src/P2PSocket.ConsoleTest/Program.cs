using P2PSocket.Core.Models;
using P2PSocket.Core.Extends;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Net;

namespace P2PSocket.ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Guid guid1 = Guid.NewGuid();
            Guid guid2 = guid1;
            bool flag = guid1 == guid2;
            guid1 = Guid.NewGuid();
        }
    }
}
