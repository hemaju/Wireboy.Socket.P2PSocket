using P2PSocket.Core;
using P2PSocket.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace P2PSocket.Server.Models.Recieve
{
    /// <summary>
    ///     P2PCommandType.P2P0x0212
    /// </summary>
    public class Port2PPacket : RecievePacket
    {
        public Port2PPacket() : base()
        {
            CommandType = P2PCommandType.P2P0x0212;
        }

        public override bool ParseData(ref byte[] data)
        {
            this.DataBuffer = new byte[data.Length + 1];
            BinaryWriter writer = new BinaryWriter(new MemoryStream(DataBuffer));
            //是否非Client与Server发送数据
            writer.Write(true);
            writer.Write(data);
            writer.Close();
            data = new byte[0];
            return true;
        }
    }
}
