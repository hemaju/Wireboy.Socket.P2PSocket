using P2PSocket.Core.Commands;
using P2PSocket.Core.Models;
using P2PSocket.Client.Models.Send;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using P2PSocket.Core.Utils;
using P2PSocket.Client.Utils;
using P2PSocket.Core.Enums;

namespace P2PSocket.Client.Commands
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
            LogUtils.Trace($"开始处理消息：0x0301");
            LogLevel logLevel = BinaryUtils.ReadLogLevel(m_data);
            string msg = BinaryUtils.ReadString(m_data);
            string sourceName = BinaryUtils.ReadString(m_data);
            LogUtils.WriteLine(logLevel, $"命令：0x0301 接收到{sourceName}的消息-> {msg}");
            return true;
        }
    }
}
