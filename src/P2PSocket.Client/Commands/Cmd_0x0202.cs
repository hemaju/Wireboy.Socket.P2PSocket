using P2PSocket.Client.Models.Send;
using P2PSocket.Core.Commands;
using P2PSocket.Core.Models;
using System.IO;
using System;
using System.Collections.Generic;
using System.Text;
using P2PSocket.Core.Utils;
using P2PSocket.Client.Utils;

namespace P2PSocket.Client.Commands
{
    [CommandFlag(Core.P2PCommandType.P2P0x0202)]
    public class Cmd_0x0202 : P2PCommand
    {
        readonly P2PTcpClient m_tcpClient;
        BinaryReader m_data { get; }
        public Cmd_0x0202(P2PTcpClient tcpClient, byte[] data)
        {
            m_tcpClient = tcpClient;
            m_data = new BinaryReader(new MemoryStream(data));
        }
        public override bool Excute()
        {
            LogUtils.Debug($"命令：0x0202 P2P（3端）数据转发 From:{m_tcpClient.ToClient.RemoteEndPoint} Length:{((MemoryStream)m_data.BaseStream).Length}");

            //是否来自端口
            if (BinaryUtils.ReadBool(m_data))
            {
                //Port->Client
                Send_0x0202 sendPacket = new Send_0x0202(BinaryUtils.ReadBytes(m_data), false);
                m_tcpClient.ToClient.BeginSend(sendPacket.PackData());
            }
            else
            {
                //Server->Client
                m_tcpClient.ToClient.BeginSend(BinaryUtils.ReadBytes(m_data));
            }
            return true;
        }
    }
}
