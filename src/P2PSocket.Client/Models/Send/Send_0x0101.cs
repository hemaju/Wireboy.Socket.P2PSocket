using P2PSocket.Core;
using P2PSocket.Core.Models;
using P2PSocket.Core.Extends;
using System;
using System.Collections.Generic;
using System.Text;
using P2PSocket.Core.Utils;

namespace P2PSocket.Client.Models.Send
{
    public class Send_0x0101 : SendPacket
    {
        public Send_0x0101() : base(P2PCommandType.Login0x0101)
        {
            AppCenter appCenter = EasyInject.Get<AppCenter>();
            //  客户端名称
            BinaryUtils.Write(Data, appCenter.Config.ClientName);
            //  授权码
            BinaryUtils.Write(Data, appCenter.Config.AuthCode);
        }
    }
}
