using P2PSocket.Core.Commands;
using P2PSocket.Core.Extends;
using P2PSocket.Core.Models;
using P2PSocket.Server.Models.Recieve;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace P2PSocket.Server.Commands
{
    [CommandFlag(Core.P2PCommandType.P2P0x0211)]
    public class Port2PApplyCommand : P2PCommand
    {
        readonly P2PTcpClient m_tcpClient;
        BinaryReader m_data { get; }
        public Port2PApplyCommand(P2PTcpClient tcpClient, byte[] data)
        {
            m_tcpClient = tcpClient;
            m_data = new BinaryReader(new MemoryStream(data));
        }
        public override bool Excute()
        {
            int tokenLength = m_data.ReadInt32();
            string token = m_data.ReadBytes(tokenLength).ToStringUnicode();
            if (Global.WaiteConnetctTcp.ContainsKey(token))
            {
                P2PTcpClient client = Global.WaiteConnetctTcp[token];
                Global.WaiteConnetctTcp.Remove(token);
                client.IsAuth = m_tcpClient.IsAuth = true;
                client.ToClient = m_tcpClient;
                m_tcpClient.ToClient = client;
                Debug.WriteLine($"[服务器]转发{client.RemoteEndPoint}->{m_tcpClient.RemoteEndPoint}");
                //监听client
                Global.TaskFactory.StartNew(() => { Global_Func.ListenTcp<Port2PPacket>(client); });
            }
            else
            {
                m_tcpClient.Close();
                throw new Exception("连接已关闭");
            }
            return true;
        }
    }
}
