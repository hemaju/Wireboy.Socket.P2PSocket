using P2PSocket.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace P2PSocket.Client.Models.Send
{
    public class P2PortPacket : SendPacket
    {
        public P2PortPacket(byte[] data)
        {
            Data.Write(data);
        }

        public override byte[] PackData()
        {
            return ((MemoryStream)Data.BaseStream).ToArray();
        }
    }
}
