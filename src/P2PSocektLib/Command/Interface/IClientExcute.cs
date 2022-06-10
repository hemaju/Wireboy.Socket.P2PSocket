using P2PSocektLib.Export;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PSocektLib.Command
{
    internal interface IClientExcute
    {
        Task Handle(P2PClient client, P2PConnect conn, byte[] data, uint token);
    }
}
