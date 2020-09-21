using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using P2PSocket.Core.Commands;

namespace P2PSocket.Core.Models
{
    public class PipeRecievePacket : ReceivePacket
    {
        protected override bool ParseCommand(byte[] data)
        {
            return true;
        }
    }
}
