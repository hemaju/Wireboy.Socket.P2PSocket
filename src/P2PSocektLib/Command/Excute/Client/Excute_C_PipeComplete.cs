using P2PSocektLib.Export;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PSocektLib.Command.Excute.Client
{
    [ClientExcute(RequestEnum.管道_通知握手完成)]
    internal class Excute_C_PipeComplete : IClientExcute
    {
        public Task Handle(P2PClient client, P2PConnect conn, byte[] data, uint token)
        {
            throw new NotImplementedException();
        }
    }
}
