using P2PSocket.Core;
using P2PSocket.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace P2PSocket.Server.Models.Receive
{
    /// <summary>
    ///     P2PCommandType.P2P0x0212
    /// </summary>
    public class P2PortPacket : ReceivePacket
    {
        public P2PortPacket() : base()
        {
            
        }

        public override bool ParseData(ref byte[] data)
        {
            this.Data = new byte[data.Length];
            data.CopyTo(this.Data, 0);
            data = new byte[0];
            return true;
        }
    }
}
