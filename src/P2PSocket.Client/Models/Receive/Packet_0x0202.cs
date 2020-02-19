using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace P2PSocket.Client.Models.Receive
{
    public class Packet_0x0202 : ReceivePacket
    {
        public Packet_0x0202() : base()
        {
        }

        public override bool ParseData(ref byte[] data)
        {
            CommandType = Core.P2PCommandType.P2P0x0202;
            BinaryWriter writer = new BinaryWriter(new MemoryStream());
            //来自外部
            BinaryUtils.Write(writer, true);
            BinaryUtils.Write(writer, data);
            this.Data = ((MemoryStream)writer.BaseStream).ToArray();
            writer.Close();
            data = new byte[0];
            return true;
        }
    }
}
