using P2PSocket.Core;
using P2PSocket.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Server.Models.Send
{
    public class P2PTransferPacket :SendPacket
    {
        public P2PTransferPacket(byte[] data) : base(P2PCommandType.P2P0x0202)
        {
            Data.Write(data);
        }
    }
}
