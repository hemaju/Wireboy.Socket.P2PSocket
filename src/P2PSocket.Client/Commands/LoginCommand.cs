using P2PSocket.Core.Commands;
using P2PSocket.Core.Models;
using P2PSocket.Core.Extends;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace P2PSocket.Client.Commands
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
            if (m_data.ReadBoolean())
            {
                //身份验证成功
                int intTemp = 0;
                string strTemp = "";
                intTemp = m_data.ReadInt32();
                strTemp = m_data.ReadBytes(intTemp).ToStringUnicode();
                Debug.WriteLine($"身份认证成功,服务名:{strTemp}");
                intTemp = m_data.ReadInt32();
                Global.P2PServerTcp.Token = m_data.ReadBytes(intTemp).ToStringUnicode();
            }
            else
            {
                //身份验证失败
                Debug.WriteLine("身份认证失败");
            }
            return true;
        }
    }
}
