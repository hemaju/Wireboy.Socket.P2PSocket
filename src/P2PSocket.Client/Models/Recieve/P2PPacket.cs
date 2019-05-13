using P2PSocket.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace P2PSocket.Client.Models.Recieve
{
    public class P2PPacket : RecievePacket
    {
        public P2PPacket() : base()
        {
            CommandType = Core.P2PCommandType.P2P0x0202;
        }

        public override bool ParseData(ref byte[] data)
        {
            BinaryWriter writer = new BinaryWriter(new MemoryStream());
            //来自外部
            writer.Write(true);
            writer.Write(data);
            this.DataBuffer = ((MemoryStream)writer.BaseStream).ToArray();
            writer.Close();
            data = new byte[0];
            return true;
        }
    }
}
