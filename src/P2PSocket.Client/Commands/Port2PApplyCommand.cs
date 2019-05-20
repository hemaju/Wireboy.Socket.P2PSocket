using P2PSocket.Core.Commands;
using P2PSocket.Core.Extends;
using P2PSocket.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace P2PSocket.Client.Commands
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
            int mapPort = m_data.ReadInt32();
            if (Global.AllowPort.Contains(mapPort))
            {
                P2PTcpClient portClient = new P2PTcpClient("127.0.0.1", mapPort);
                P2PTcpClient serverClient = new P2PTcpClient(Global.ServerAddress, Global.ServerPort);
                portClient.IsAuth = serverClient.IsAuth = true;
                portClient.ToClient = serverClient;
                serverClient.ToClient = portClient;


                Models.Send.P2PortApplyResponse sendPacket = new Models.Send.P2PortApplyResponse(token);
                int length = serverClient.Client.Send(sendPacket.PackData());
                Debug.WriteLine($"Port2P请求有效，{portClient.LocalEndPoint}->{serverClient.LocalEndPoint}");
                Global.TaskFactory.StartNew(() => { Global_Func.ListenTcp<Models.Recieve.Port2PPacket>(portClient); });
                Global.TaskFactory.StartNew(() => { Global_Func.ListenTcp<Models.Recieve.P2PortPacket>(serverClient); });
            }
            else
            {
                Debug.WriteLine("Port2P请求无效，关闭连接");
                m_tcpClient.Close();
            }
            return true;
        }
    }
}
