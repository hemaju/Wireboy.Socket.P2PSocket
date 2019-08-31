using System;
using System.Collections.Generic;
using P2PSocket.Core.Extends;
using P2PSocket.Core.Models;
using P2PSocket.Core;
using System.Text;
using P2PSocket.Core.Utils;

namespace P2PSocket.Client.Models.Send
{
    public class Send_0x0202 : SendPacket
    {
        public Send_0x0202(byte[] data,bool isFromPort) : base(P2PCommandType.P2P0x0202)
        {
            //  是否来自端口
            BinaryUtils.Write(Data, isFromPort);
            //  数据块
            BinaryUtils.Write(Data, data);
        }
    }
}
