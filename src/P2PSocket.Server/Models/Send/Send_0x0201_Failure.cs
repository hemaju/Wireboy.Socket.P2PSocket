using P2PSocket.Core;
using P2PSocket.Core.Models;
using P2PSocket.Core.Extends;
using System;
using System.Collections.Generic;
using System.Text;
using P2PSocket.Core.Utils;

namespace P2PSocket.Server.Models.Send
{
    public class Send_0x0201_Failure : SendPacket
    {
        public Send_0x0201_Failure(string message) : base(P2PCommandType.P2P0x0201)
        {
            BinaryUtils.Write(Data, (int)-1);
            BinaryUtils.Write(Data, message);
        }
    }
}
