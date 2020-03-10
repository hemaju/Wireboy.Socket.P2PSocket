using P2PSocket.Core.Commands;
using P2PSocket.Core.Extends;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using P2PSocket.Server.Models.Receive;
using P2PSocket.Server.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace P2PSocket.Server.Commands
{
    [CommandFlag(Core.P2PCommandType.P2P0x0211)]
    public class Cmd_0x0211 : P2PCommand
    {
        readonly P2PTcpClient m_tcpClient;
        BinaryReader m_data { get; }
        public Cmd_0x0211(P2PTcpClient tcpClient, byte[] data) 
        {
            m_tcpClient = tcpClient;
            m_data = new BinaryReader(new MemoryStream(data));
        }
        public override bool Excute()
        {
            int tokenLength = m_data.ReadInt32();
            string token = m_data.ReadBytes(tokenLength).ToStringUnicode();
            if (ClientCenter.Instance.WaiteConnetctTcp.ContainsKey(token))
            {
                P2PTcpClient client = ClientCenter.Instance.WaiteConnetctTcp[token];
                ClientCenter.Instance.WaiteConnetctTcp.Remove(token);
                client.IsAuth = m_tcpClient.IsAuth = true;
                client.ToClient = m_tcpClient;
                m_tcpClient.ToClient = client;
                LogUtils.Debug($"命令：0x0211 已绑定内网穿透（2端）通道 {client.RemoteEndPoint}->{m_tcpClient.RemoteEndPoint}");
                //监听client
                AppCenter.Instance.StartNewTask(() => { Global_Func.ListenTcp<Packet_0x0212>(client); });
            }
            else
            {
                m_tcpClient.SafeClose();
                throw new Exception("绑定内网穿透（2端）通道失败，目标Tcp连接已断开");
            }
            return true;
        }
    }
}
