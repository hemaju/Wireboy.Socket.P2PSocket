using P2PSocket.Core.Commands;
using P2PSocket.Core.Models;
using P2PSocket.Client.Models.Send;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using P2PSocket.Core.Utils;
using P2PSocket.Client.Utils;

namespace P2PSocket.Client.Commands
{
    [CommandFlag(Core.P2PCommandType.P2P0x0212)]
    public class Cmd_0x0212 : P2PCommand
    {
        readonly P2PTcpClient m_tcpClient;
        BinaryReader m_data { get; }
        public Cmd_0x0212(P2PTcpClient tcpClient, byte[] data)
        {
            m_tcpClient = tcpClient;
            m_data = new BinaryReader(new MemoryStream(data));
        }
        public override bool Excute()
        {
            LogUtils.Debug($"命令：0x0212 P2P（2端）数据转发 From:{m_tcpClient.ToClient.RemoteEndPoint} Length:{((MemoryStream)m_data.BaseStream).Length}");
            if (BinaryUtils.ReadBool(m_data))
            {
                //Port->Client
                Send_0x0212 sendPacket = new Send_0x0212(BinaryUtils.ReadBytes(m_data));
                m_tcpClient.ToClient.Client.Send(sendPacket.PackData());
            }
            else
            {
                //Server->Client
                Send_Transfer sendPacket = new Send_Transfer(BinaryUtils.ReadBytes(m_data));
                m_tcpClient.ToClient.Client.Send(sendPacket.PackData());
            }
            return true;
        }
    }
}
