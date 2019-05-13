using P2PSocket.Core.Commands;
using P2PSocket.Core.Models;
using P2PSocket.Core.Extends;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using P2PSocket.Server.Models.Send;

namespace P2PSocket.Server.Commands
{
    [CommandFlag(Core.P2PCommandType.Login0x0101)]
    public class LoginCommand : P2PCommand
    {
        readonly P2PTcpClient m_tcpClient;
        BinaryReader m_data { get; }
        public LoginCommand(P2PTcpClient tcpClient, byte[] data)
        {
            m_tcpClient = tcpClient;
            m_data = new BinaryReader(new MemoryStream(data));
        }
        public override bool Excute()
        {
            int clientNameLength = m_data.ReadInt32();
            string clientName = m_data.ReadBytes(clientNameLength).ToStringUnicode();
            m_tcpClient.IsAuth = true;
            if (Global.TcpMap.ContainsKey(clientName))
                Global.TcpMap[clientName] = m_tcpClient;
            else
                Global.TcpMap.Add(clientName, m_tcpClient);
            SendResponse(clientName);
            return true;
        }

        private void SendResponse(string clientName)
        {
            LoginResponse sendPacket = new LoginResponse(m_tcpClient, clientName);
            m_tcpClient.Client.Send(sendPacket.PackData());
            Debug.WriteLine($"服务器：成功接入Client:{clientName}");
        }
    }
}
