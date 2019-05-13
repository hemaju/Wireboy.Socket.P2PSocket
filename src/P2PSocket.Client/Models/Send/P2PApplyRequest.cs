using P2PSocket.Core;
using P2PSocket.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using P2PSocket.Core.Extends;

namespace P2PSocket.Client.Models.Send
{
    /// <summary>
    ///     P2PCommandType.P2P0x0201
    /// </summary>
    public class P2PApplyRequest : SendPacket
    {
        public P2PApplyRequest(string token, string clientName, int port) : base(P2PCommandType.P2P0x0201)
        {
            //是否第一步
            Data.Write((int)1);
            Data.Write((int)token.ToBytes().Length);
            Data.Write(token.ToBytes());
            Data.Write((int)clientName.ToBytes().Length);
            Data.Write(clientName.ToBytes());
            Data.Write(port);
        }

        public P2PApplyRequest(string token) : base(P2PCommandType.P2P0x0201)
        {
            Data.Write((int)3);
            Data.Write((int)token.ToBytes().Length);
            Data.Write(token.ToBytes());
        }
    }
}
