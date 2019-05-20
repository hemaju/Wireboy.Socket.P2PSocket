using P2PSocket.Core;
using P2PSocket.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using P2PSocket.Core.Extends;

namespace P2PSocket.Server.Models.Send
{
    /// <summary>
    ///     P2PCommandType.P2P0x0212
    /// </summary>
    public class Port2PApplyRequest : SendPacket
    {
        public Port2PApplyRequest(string token, int port) : base(P2PCommandType.P2P0x0211)
        {
            Data.Write((int)token.ToBytes().Length);
            Data.Write(token.ToBytes());
            Data.Write((int)port);
        }
    }
}
