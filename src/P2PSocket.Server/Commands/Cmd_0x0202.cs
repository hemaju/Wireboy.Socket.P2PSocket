using P2PSocket.Core.Commands;
using P2PSocket.Core.Models;
using P2PSocket.Server.Models.Send;
using System;
using System.Collections.Generic;
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
            Send_0x0202 sendPacket = new Send_0x0202(m_data);
            m_tcpClient.ToClient.Client.Send(sendPacket.PackData());
            return true;
        }
    }
}
