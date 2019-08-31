using P2PSocket.Core;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Client.Models.Send
{
    public class Send_0x0052 : SendPacket
    {
        public Send_0x0052() : base(P2PCommandType.Heart0x0052)
        {
            //  心跳包数据
            //BinaryUtils.Write(Data, (int)0x0052);
            BinaryUtils.Write(Data, "心跳包");
        }
    }
}
