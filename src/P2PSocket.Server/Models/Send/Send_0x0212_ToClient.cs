using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Server.Models.Send
{
    public class Send_0x0212_ToClient : SendPacket
    {
        public Send_0x0212_ToClient(byte[] data) : base(Core.P2PCommandType.P2P0x0212)
        {
            BinaryUtils.Write(Data, false);
            BinaryUtils.Write(Data, data);
        }
    }
}
