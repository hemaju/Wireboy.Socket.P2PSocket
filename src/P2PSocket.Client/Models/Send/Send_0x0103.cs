using P2PSocket.Core;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Client.Models.Send
{
    public class Send_0x0103 : SendPacket
    {
        public Send_0x0103() : base(P2PCommandType.Login0x0103)
        {
            //  allowport
            BinaryUtils.Write(Data, ConfigCenter.Instance.AllowPortList);
            //  客户端黑名单
            BinaryUtils.Write(Data, ConfigCenter.Instance.BlackClients);
        }
    }
}
