using P2PSocket.Core;
using P2PSocket.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using P2PSocket.Core.Extends;
using P2PSocket.Core.Utils;

namespace P2PSocket.Client.Models.Send
{
    /// <summary>
    ///     P2PCommandType.P2P0x0201
    /// </summary>
    public class Send_0x0201_Apply : SendPacket
    {
        public Send_0x0201_Apply(string token, string clientName, int port, int p2pType) : base(P2PCommandType.P2P0x0201)
        {
            //  P2P申请标志
            BinaryUtils.Write(Data, (int)1);
            //  P2P穿透唯一标识
            BinaryUtils.Write(Data, token);
            //  目标客户端名称
            BinaryUtils.Write(Data, clientName);
            //  目标端口
            BinaryUtils.Write(Data, port);
            //  P2P类型
            BinaryUtils.Write(Data, p2pType);
            //BinaryUtils.Write(Data, 1);


        }
    }
}
