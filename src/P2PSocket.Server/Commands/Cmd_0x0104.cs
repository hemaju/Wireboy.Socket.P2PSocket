using P2PSocket.Core.Commands;
using P2PSocket.Core.Extends;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using P2PSocket.Server.Models;
using P2PSocket.Server.Models.Send;
using P2PSocket.Server.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace P2PSocket.Server.Commands
{
    [CommandFlag(Core.P2PCommandType.Login0x0104)]
    public class Cmd_0x0104 : P2PCommand
    {
        readonly P2PTcpClient m_tcpClient;
        BinaryReader m_data { get; }
        AppCenter appCenter = EasyInject.Get<AppCenter>();
        ClientCenter clientCenter = EasyInject.Get<ClientCenter>();
        public Cmd_0x0104(P2PTcpClient tcpClient, byte[] data)
        {
            m_tcpClient = tcpClient;
            m_data = new BinaryReader(new MemoryStream(data));
        }
        public override bool Excute()
        {
            LogUtils.Trace($"开始处理消息：0x0104");
            bool ret = true;
            string macAddress = BinaryUtils.ReadString(m_data);
            string clientName = clientCenter.GetClientName(macAddress);
            bool isSuccess = true;
            P2PTcpItem item = new P2PTcpItem();
            item.TcpClient = m_tcpClient;
            if (appCenter.Config.ClientAuthList.Count == 0 || appCenter.Config.ClientAuthList.Any(t => t.Match(clientName, "auto")))
            {
                if (clientCenter.TcpMap.ContainsKey(clientName))
                {
                    if (clientCenter.TcpMap[clientName].TcpClient.IsDisConnected)
                    {
                        clientCenter.TcpMap[clientName].TcpClient?.SafeClose();
                        clientCenter.TcpMap[clientName] = item;
                    }
                    else
                    {
                        isSuccess = false;
                        Send_0x0101 sendPacket = new Send_0x0101(m_tcpClient, false, $"ClientName:{clientName} 已被使用", clientName);
                        m_tcpClient.BeginSend(sendPacket.PackData());
                        ret = false;
                    }
                }
                else
                    clientCenter.TcpMap.Add(clientName, item);
                if (isSuccess)
                {
                    m_tcpClient.ClientName = clientName;
                    Send_0x0101 sendPacket = new Send_0x0101(m_tcpClient, true, $"客户端{clientName}认证通过", clientName);
                    m_tcpClient.BeginSend(sendPacket.PackData());
                }
            }
            else
            {
                Send_0x0101 sendPacket = new Send_0x0101(m_tcpClient, false, $"客户端{clientName}认证失败", clientName);
                m_tcpClient.BeginSend(sendPacket.PackData());
                ret = false;
            }
            return ret;
        }
    }
}
