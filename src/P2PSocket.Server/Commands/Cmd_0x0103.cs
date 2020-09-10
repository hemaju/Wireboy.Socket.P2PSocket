using P2PSocket.Core.Commands;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using P2PSocket.Server.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace P2PSocket.Server.Commands
{
    [CommandFlag(Core.P2PCommandType.Login0x0103)]
    public class Cmd_0x0103 : P2PCommand
    {
        readonly P2PTcpClient m_tcpClient;
        BinaryReader m_data { get; }
        ClientCenter clientCenter = EasyInject.Get<ClientCenter>();
        public Cmd_0x0103(P2PTcpClient tcpClient, byte[] data)
        {
            m_tcpClient = tcpClient;
            m_data = new BinaryReader(new MemoryStream(data));
        }
        public override bool Excute()
        {
            LogUtils.Trace($"开始处理消息：0x0103");
            if (clientCenter.TcpMap.ContainsKey(m_tcpClient.ClientName))
            {
                P2PTcpItem item = clientCenter.TcpMap[m_tcpClient.ClientName];
                item.AllowPorts = BinaryUtils.ReadObjectList<AllowPortItem>(m_data);
                item.BlackClients = BinaryUtils.ReadStringList(m_data);
            }
            return true;
        }
    }
}
