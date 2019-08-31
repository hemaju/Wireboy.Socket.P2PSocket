using P2PSocket.Core.Commands;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using P2PSocket.Server.Models.Send;
using P2PSocket.Server.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace P2PSocket.Server.Commands
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
            if (BinaryUtils.ReadBool(m_data))
            {
                //Port->Client
                LogUtils.Debug("[服务器]Port->Client");
                Send_0x0212_ToClient sendPacket = new Send_0x0212_ToClient(BinaryUtils.ReadBytes(m_data));
                m_tcpClient.ToClient.Client.Send(sendPacket.PackData());
            }
            else
            {
                //Client->Port
                LogUtils.Debug("[服务器]Client->Port");
                Send_0x0212_ToPort sendPacket = new Send_0x0212_ToPort(BinaryUtils.ReadBytes(m_data));
                m_tcpClient.ToClient.Client.Send(sendPacket.PackData());
            }
            return true;
        }
    }
}
