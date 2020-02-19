using P2PSocket.Core;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace P2PSocket.Client.Models.Receive
{
    /// <summary>
    ///     P2PCommandType.P2P0x0212
    /// </summary>
    public class Packet_0x0212 : ReceivePacket
    {
        public Packet_0x0212() : base()
        {
        }

        public override bool ParseData(ref byte[] data)
        {
            CommandType = P2PCommandType.P2P0x0212;
            BinaryWriter writer = new BinaryWriter(new MemoryStream());
            BinaryUtils.Write(writer, true);
            BinaryUtils.Write(writer, data);
            writer.Close();
            this.Data = ((MemoryStream)writer.BaseStream).ToArray();
            data = new byte[0];
            return true;
        }
    }
}
