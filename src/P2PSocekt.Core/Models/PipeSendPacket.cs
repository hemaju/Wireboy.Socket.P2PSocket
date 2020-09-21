using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace P2PSocket.Core.Models
{
    public class PipeSendPacket : SendPacket
    {
        public PipeSendPacket(byte[] data)
        {
            Data.Write(data);
        }
        public PipeSendPacket() : base()
        {

        }

        protected override void SetCommandType(BinaryWriter writer)
        {
            //置空
        }
    }
}
