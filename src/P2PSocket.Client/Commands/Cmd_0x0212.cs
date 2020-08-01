using P2PSocket.Core.Commands;
using P2PSocket.Core.Models;
using P2PSocket.Client.Models.Send;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using P2PSocket.Core.Utils;
using P2PSocket.Client.Utils;
using P2PSocket.Core.Extends;

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
            LogUtils.Trace($"开始处理消息：0x0212 From:{m_tcpClient.ToClient.RemoteEndPoint} Length:{((MemoryStream)m_data.BaseStream).Length}");
            bool ret = true;
            if (BinaryUtils.ReadBool(m_data))
            {
                //Port->Client
                Send_0x0212 sendPacket = new Send_0x0212(BinaryUtils.ReadBytes(m_data));
                EasyOp.Do(() =>
                {
                    m_tcpClient.ToClient.BeginSend(sendPacket.PackData());
                }, ex =>
                {
                    ret = false;
                    LogUtils.Debug($"命令：0x0212 转发来自端口的数据失败：{Environment.NewLine}{ex}");
                });
            }
            else
            {
                //Server->Client
                Send_Transfer sendPacket = new Send_Transfer(BinaryUtils.ReadBytes(m_data));
                EasyOp.Do(() =>
                {
                    m_tcpClient.ToClient.BeginSend(sendPacket.PackData());
                }, ex =>
                {
                    ret = false;
                    LogUtils.Debug($"命令：0x0212 转发来自端口的数据失败：{Environment.NewLine}{ex}");
                });
            }
            return ret;
        }
    }
}
