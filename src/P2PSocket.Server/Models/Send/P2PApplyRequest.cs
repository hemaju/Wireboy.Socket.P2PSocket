using P2PSocket.Core;
using P2PSocket.Core.Models;
using P2PSocket.Core.Extends;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Server.Models.Send
{
    public class P2PApplyRequest : SendPacket
    {
        public P2PApplyRequest(string token, int port) : base(P2PCommandType.P2P0x0201)
        {
            Data.Write((int)2);
            Data.Write(true);
            Data.Write((int)token.ToBytes().Length);
            Data.Write(token.ToBytes());
            Data.Write(port);
        }
        public P2PApplyRequest(string token) : base(P2PCommandType.P2P0x0201)
        {
            Data.Write((int)2);
            Data.Write(false);
            Data.Write((int)token.ToBytes().Length);
            Data.Write(token.ToBytes());
        }
        public P2PApplyRequest() : base(P2PCommandType.P2P0x0201)
        {
            Data.Write((int)4);
        }
    }
}
