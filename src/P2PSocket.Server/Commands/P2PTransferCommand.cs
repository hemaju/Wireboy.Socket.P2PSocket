using P2PSocket.Core.Commands;
using P2PSocket.Core.Models;
using P2PSocket.Server.Models.Send;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Server.Commands
{
    [CommandFlag(Core.P2PCommandType.P2P0x0202)]
    public class P2PTransferCommand : P2PCommand
    {
        readonly P2PTcpClient m_tcpClient;
        byte[] m_data { get; }
        public P2PTransferCommand(P2PTcpClient tcpClient, byte[] data)
        {
            m_tcpClient = tcpClient;
            m_data = data;
        }
        public override bool Excute()
        {
            P2PTransferPacket sendPacket = new P2PTransferPacket(m_data);
            m_tcpClient.ToClient.Client.Send(sendPacket.PackData());
            return true;
        }
    }
}
