using P2PSocket.Core.Commands;
using P2PSocket.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Server.Commands
{
    [CommandFlag(Core.P2PCommandType.Login0x0102)]
    public class Cmd_0x0102 : P2PCommand
    {
        readonly P2PTcpClient m_tcpClient;
        byte[] m_data { get; }
        public Cmd_0x0102(P2PTcpClient tcpClient, byte[] data)
        {
            m_tcpClient = tcpClient;
            m_data = data;
        }
        public override bool Excute()
        {
            LogUtils.Trace($"开始处理消息：0x0102");
            return true;
        }
    }
}
