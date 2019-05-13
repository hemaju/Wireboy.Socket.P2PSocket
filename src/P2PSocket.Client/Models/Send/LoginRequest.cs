using P2PSocket.Core;
using P2PSocket.Core.Models;
using P2PSocket.Core.Extends;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Client.Models.Send
{
    public class LoginRequest : SendPacket
    {
        public LoginRequest() : base(P2PCommandType.Login0x0101)
        {
            InitData();
        }
        private void InitData()
        {
            Data.Write((int)Global.ClientName.ToBytes().Length);
            Data.Write(Global.ClientName.ToBytes());
        }
    }
}
