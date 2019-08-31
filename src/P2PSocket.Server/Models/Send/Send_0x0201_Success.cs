using P2PSocket.Core;
using P2PSocket.Core.Models;
using P2PSocket.Core.Extends;
using System;
using System.Collections.Generic;
using System.Text;
using P2PSocket.Core.Utils;

namespace P2PSocket.Server.Models.Send
{
    public class Send_0x0201_Success : SendPacket
    {
        public Send_0x0201_Success(string token, int port) : base(P2PCommandType.P2P0x0201)
        {
            BinaryUtils.Write(Data, (int)2);
            BinaryUtils.Write(Data, true);
            BinaryUtils.Write(Data, token);
            BinaryUtils.Write(Data, port);
        }
        public Send_0x0201_Success(string token) : base(P2PCommandType.P2P0x0201)
        {
            BinaryUtils.Write(Data, (int)2);
            BinaryUtils.Write(Data, false);
            BinaryUtils.Write(Data, token);
        }
        public Send_0x0201_Success() : base(P2PCommandType.P2P0x0201)
        {
            BinaryUtils.Write(Data, (int)4);
        }
    }
}
