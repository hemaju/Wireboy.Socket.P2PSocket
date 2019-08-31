using P2PSocket.Core;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace P2PSocket.Server.Models.Send
{
    /// <summary>
    ///     P2PCommandType.P2P0x0212
    /// </summary>
    public class Send_0x0212_ToPort : SendPacket
    {
        public Send_0x0212_ToPort(byte[] data) : base(P2PCommandType.P2P0x0212) 
        {
            Data.Write(data);
        }

        /// <summary>
        ///     打包数据，仅包含data
        /// </summary>
        /// <returns></returns>
        public override byte[] PackData()
        {
            return ((MemoryStream)Data.BaseStream).ToArray();
        }
    }
}
