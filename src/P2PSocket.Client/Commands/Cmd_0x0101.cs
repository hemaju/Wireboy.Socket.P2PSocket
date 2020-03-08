using P2PSocket.Core.Commands;
using P2PSocket.Core.Models;
using P2PSocket.Core.Extends;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using P2PSocket.Client.Utils;
using P2PSocket.Core.Utils;
using P2PSocket.Client.Models.Send;

namespace P2PSocket.Client.Commands
{
    [CommandFlag(Core.P2PCommandType.Login0x0101)]
    public class Cmd_0x0101 : P2PCommand
    {
        readonly P2PTcpClient m_tcpClient;
        BinaryReader m_data { get; }
        public Cmd_0x0101(P2PTcpClient tcpClient, byte[] data)
        {
            m_tcpClient = tcpClient;
            m_data = new BinaryReader(new MemoryStream(data));
        }
        public override bool Excute()
        {
            if (IsSuccess())
                DoSuccess();
            else
                DoFailure();
            return true;
        }

        public bool IsSuccess()
        {
            return m_data.ReadBoolean();
        }

        public void DoSuccess()
        {
            //  身份验证成功
            string msg = BinaryUtils.ReadString(m_data);
            LogUtils.Info($"命令：0x0101 {msg}");
            TcpCenter.Instance.P2PServerTcp.Token = BinaryUtils.ReadString(m_data);
            //  发送客户端信息
            Send_0x0103 sendPacket = new Send_0x0103();
            Utils.LogUtils.Info("命令：0x0101 同步服务端数据");
            m_tcpClient.Client.Send(sendPacket.PackData());
        }

        public void DoFailure()
        {
            //身份验证失败
            string msg = BinaryUtils.ReadString(m_data);
            LogUtils.Error($"命令：0x0101 {msg}");
        }
    }
}
