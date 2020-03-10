using P2PSocket.Core.Commands;
using P2PSocket.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using P2PSocket.Core.Utils;
using P2PSocket.Server.Models.Send;

namespace P2PSocket.Server.Commands
{
    [CommandFlag(Core.P2PCommandType.Msg0x0301)]
    public class Cmd_0x0301 : P2PCommand
    {
        readonly P2PTcpClient m_tcpClient;
        BinaryReader m_data { get; }
        public Cmd_0x0301(P2PTcpClient tcpClient, byte[] data)
        {
            m_tcpClient = tcpClient;
            m_data = new BinaryReader(new MemoryStream(data));
        }
        public override bool Excute()
        {
            LogLevel logLevel = BinaryUtils.ReadLogLevel(m_data);
            string msg = BinaryUtils.ReadString(m_data);
            string destName = BinaryUtils.ReadString(m_data);
            if (string.IsNullOrEmpty(destName))
            {
                //  给服务端的消息
                LogUtils.WriteLine(logLevel, msg);
            }
            else
            {
                //  给指定客户端的消息
                if (ClientCenter.Instance.TcpMap.ContainsKey(destName))
                {
                    //  将消息转发至指定客户端
                    Msg_0x0301 sendPacket = new Msg_0x0301(logLevel, msg, m_tcpClient.ClientName);
                    ClientCenter.Instance.TcpMap[destName].TcpClient.Client.Send(sendPacket.PackData());
                }
                else
                {
                    //  指定客户端不在线
                    LogUtils.WriteLine(logLevel, $"To_{destName}:{msg}");
                }
            }

            return true;
        }
    }
}
