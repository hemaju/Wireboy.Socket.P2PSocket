using P2PSocket.Core;
using P2PSocket.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Client.Models.Send
{
    public class HeartPacket:SendPacket
    {
        public HeartPacket():base(P2PCommandType.Heart0x0052)
        {
            Data.Write((int)0x0052);
        }
    }
}
