using P2PSocket.Core.Commands;
using P2PSocket.Core.Extends;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using P2PSocket.Server.Models.Send;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace P2PSocket.Server.Commands
{
    [CommandFlag(Core.P2PCommandType.P2P0x0202)]
    public class Cmd_0x0202 : P2PCommand
    {
        readonly P2PTcpClient m_tcpClient;
        byte[] m_data { get; }
        public Cmd_0x0202(P2PTcpClient tcpClient, byte[] data)
        {
            m_tcpClient = tcpClient;
            m_data = data;
        }
        public override bool Excute()
        {
            LogUtils.Trace($"开始处理消息：0x0202");
            Send_0x0202 sendPacket = new Send_0x0202(m_data);
            bool ret = true;
            EasyOp.Do(() => {
                m_tcpClient.ToClient.BeginSend(sendPacket.PackData());
            }, ex => {
                ret = false;
            });
            return ret;
        }
    }
}
