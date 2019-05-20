using P2PSocket.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Server.Models.Send
{
    public class Port2PTransfer : SendPacket
    {
        public Port2PTransfer(byte[] data) : base(Core.P2PCommandType.P2P0x0212)
        {
            Data.Write(false);
            Data.Write(data);
        }
    }
}
