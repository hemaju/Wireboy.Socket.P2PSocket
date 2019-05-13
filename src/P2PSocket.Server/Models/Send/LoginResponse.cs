using P2PSocket.Core;
using P2PSocket.Core.Models;
using P2PSocket.Core.Extends;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Server.Models.Send
{
    public class LoginResponse : SendPacket
    {
        public LoginResponse(P2PTcpClient tcpClient,string msg) : base(P2PCommandType.Login0x0101)
        {
            Data.Write(true);
            Data.Write((int)msg.ToBytes().Length);
            Data.Write(msg.ToBytes());
            Data.Write((int)tcpClient.Token.ToBytes().Length);
            Data.Write(tcpClient.Token.ToBytes());
        }
    }
}
