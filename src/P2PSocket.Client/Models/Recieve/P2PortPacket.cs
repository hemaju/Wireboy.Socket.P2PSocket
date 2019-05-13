using P2PSocket.Core;
using P2PSocket.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace P2PSocket.Client.Models.Recieve
{
    /// <summary>
    ///     P2PCommandType.P2P0x0212
    /// </summary>
    public class P2PortPacket : RecievePacket
    {
        public P2PortPacket() : base()
        {
            
        }
    }
}
