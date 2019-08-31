using P2PSocket.Core;
using P2PSocket.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using P2PSocket.Core.Extends;
using P2PSocket.Core.Utils;

namespace P2PSocket.Server.Models.Send
{
    /// <summary>
    ///     P2PCommandType.P2P0x0212
    /// </summary>
    public class Send_0x0211 : SendPacket
    {
        public Send_0x0211(string token, int port) : base(P2PCommandType.P2P0x0211)
        {
            BinaryUtils.Write(Data, token);
            BinaryUtils.Write(Data, port);
        }
    }
}
