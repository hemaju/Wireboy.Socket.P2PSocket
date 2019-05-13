using P2PSocket.Core;
using P2PSocket.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Client.Models.Send
{
    /// <summary>
    ///     P2PCommandType.P2P0x0212
    /// </summary>
    public class Port2PPacket : SendPacket
    {
        public Port2PPacket(byte[] data) : base(P2PCommandType.P2P0x0212) 
        {
            Data.Write(false);
            Data.Write(data);
        }
    }
}
