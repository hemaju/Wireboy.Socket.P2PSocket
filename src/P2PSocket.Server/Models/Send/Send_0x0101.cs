using P2PSocket.Core;
using P2PSocket.Core.Models;
using P2PSocket.Core.Extends;
using System;
using System.Collections.Generic;
using System.Text;
using P2PSocket.Core.Utils;

namespace P2PSocket.Server.Models.Send
{
    public class Send_0x0101 : SendPacket
    {
        public Send_0x0101(P2PTcpClient tcpClient, bool isSuccess, string msg, string clientName) : base(P2PCommandType.Login0x0101)
        {
            BinaryUtils.Write(Data, isSuccess);
            BinaryUtils.Write(Data, msg);
            if (isSuccess)
            {
                BinaryUtils.Write(Data, tcpClient.Token);
                BinaryUtils.Write(Data, clientName);
            }
        }
    }
}
