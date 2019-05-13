using P2PSocket.Client.Models.Send;
using P2PSocket.Core.Commands;
using P2PSocket.Core.Models;
using System.IO;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Client.Commands
{
    [CommandFlag(Core.P2PCommandType.P2P0x0202)]
    public class P2PTransferCommand : P2PCommand
    {
        readonly P2PTcpClient m_tcpClient;
        BinaryReader m_data { get; }
        public P2PTransferCommand(P2PTcpClient tcpClient, byte[] data)
        {
            m_tcpClient = tcpClient;
            m_data = new BinaryReader(new MemoryStream(data));
        }
        public override bool Excute()
        {
            Debug.WriteLine($"转发数据{m_tcpClient.ToClient.RemoteEndPoint}：长度{((MemoryStream)m_data.BaseStream).Length - 1}");
            //是否来自端口
            if (m_data.ReadBoolean())
            {
                //Port->Client
                P2PTransferPacket sendPacket = new P2PTransferPacket(m_data.ReadBytes((int)(m_data.BaseStream.Length - m_data.BaseStream.Position)), false);
                m_tcpClient.ToClient.Client.Send(sendPacket.PackData());
            }
            else
            {
                //Server->Client
                m_tcpClient.ToClient.Client.Send(m_data.ReadBytes((int)(m_data.BaseStream.Length - m_data.BaseStream.Position)));
            }
            return true;
        }
    }
}
