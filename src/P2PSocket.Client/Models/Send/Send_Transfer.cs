using P2PSocket.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace P2PSocket.Client.Models.Send
{
    public class Send_Transfer : SendPacket
    {
        public Send_Transfer(byte[] data)
        {
            Data.Write(data);
        }

        public override byte[] PackData()
        {
            return ((MemoryStream)Data.BaseStream).ToArray();
        }
    }
}
