using P2PSocket.Core;
using P2PSocket.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using P2PSocket.Core.Extends;
using P2PSocket.Core.Utils;

namespace P2PSocket.Client.Models.Send
{
    /// <summary>
    ///     P2PCommandType.P2P0x0201
    /// </summary>
    public class Send_0x0201_Bind : SendPacket
    {
        public Send_0x0201_Bind(string token) : base(P2PCommandType.P2P0x0201)
        {
            AppConfig appConfig = EasyInject.Get<AppCenter>().Config;
            //  P2P标志
            BinaryUtils.Write(Data, (int)3);
            //  客户端名称
            BinaryUtils.Write(Data, appConfig.ClientName);
            //  P2P穿透唯一标志
            BinaryUtils.Write(Data, token);
        }
    }
}
