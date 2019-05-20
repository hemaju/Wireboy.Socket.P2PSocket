using P2PSocket.Core;
using P2PSocket.Core.Models;
using P2PSocket.Core.Extends;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace P2PSocket.Client.Models.Send
{
    /// <summary>
    ///     P2PCommandType.P2P0x0212
    /// </summary>
    public class P2PortApplyResponse : SendPacket
    {
        public P2PortApplyResponse(string token) : base(P2PCommandType.P2P0x0211) 
        {
            Data.Write((int)token.ToBytes().Length);
            Data.Write(token.ToBytes());
        }
    }
}
