using P2PSocket.Core;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Client.Models.Send
{
    /// <summary>
    ///     P2PCommandType.P2P0x0212
    /// </summary>
    public class Send_0x0212 : SendPacket
    {
        public Send_0x0212(byte[] data) : base(P2PCommandType.P2P0x0212) 
        {
            BinaryUtils.Write(Data, false);
            BinaryUtils.Write(Data, data);
        }
    }
}
