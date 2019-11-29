using P2PSocket.Core;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Server.Models.Send
{
    public class Send_0x0052 : SendPacket
    {
        public Send_0x0052() : base(P2PCommandType.Heart0x0052)
        {
            BinaryUtils.Write(Data, 0);
        }
    }
}
