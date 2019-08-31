using P2PSocket.Core.Commands;
using P2PSocket.Core.Extends;
using P2PSocket.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Linq;
using P2PSocket.Core.Utils;
using P2PSocket.Client.Utils;

namespace P2PSocket.Client.Commands
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
            try
            {
                string token = BinaryUtils.ReadString(m_data);
                int mapPort = BinaryUtils.ReadInt(m_data);
                if (Global.AllowPortList.Any(t => t.Match(mapPort, m_tcpClient.ClientName)))
                {
                    P2PTcpClient portClient = new P2PTcpClient("127.0.0.1", mapPort);
                    P2PTcpClient serverClient = new P2PTcpClient(Global.ServerAddress, Global.ServerPort);
                    portClient.IsAuth = serverClient.IsAuth = true;
                    portClient.ToClient = serverClient;
                    serverClient.ToClient = portClient;


                    Models.Send.Send_0x0211 sendPacket = new Models.Send.Send_0x0211(token);
                    int length = serverClient.Client.Send(sendPacket.PackData());
                    LogUtils.Debug($"命令：0x0211  绑定Tcp：{portClient.LocalEndPoint}->{serverClient.LocalEndPoint}");
                    Global.TaskFactory.StartNew(() => { Global_Func.ListenTcp<Models.Receive.Packet_0x0212>(portClient); });
                    Global.TaskFactory.StartNew(() => { Global_Func.ListenTcp<Models.Receive.Packet_ToPort>(serverClient); });
                }
                else
                {
                    LogUtils.Warning($"命令：0x0211 无权限，端口:{mapPort}");
                    m_tcpClient.Close();
                }
            }
            catch (Exception ex)
            {
                LogUtils.Warning($"命令：0x0211 错误：{Environment.NewLine} {ex}");
            }
            return true;
        }
    }
}
