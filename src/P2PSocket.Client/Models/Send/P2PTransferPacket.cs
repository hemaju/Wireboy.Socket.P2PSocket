using System;
using System.Collections.Generic;
using P2PSocket.Core.Extends;
using P2PSocket.Core.Models;
using P2PSocket.Core;
using System.Text;

namespace P2PSocket.Client.Models.Send
{
    public class P2PTransferPacket : SendPacket
    {
        public P2PTransferPacket(byte[] data,bool isFromPort) : base(P2PCommandType.P2P0x0202)
        {
            Data.Write(isFromPort);
            Data.Write(data);
        }
    }
}
