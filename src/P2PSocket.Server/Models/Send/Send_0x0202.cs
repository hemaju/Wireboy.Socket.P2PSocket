using P2PSocket.Core;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Server.Models.Send
{
    public class Send_0x0202 :SendPacket
    {
        public Send_0x0202(byte[] data) : base(P2PCommandType.P2P0x0202)
        {
            //  因为这里直接将数据转发到client，所以不需要使用BinaryUtils
            //BinaryUtils.Write(Data, data);
            Data.Write(data);
        }
    }
}
