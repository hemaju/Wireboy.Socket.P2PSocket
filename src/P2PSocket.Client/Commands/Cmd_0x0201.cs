using P2PSocket.Core.Commands;
using P2PSocket.Core.Models;
using P2PSocket.Core.Extends;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net.Sockets;
using P2PSocket.Client.Models.Receive;
using P2PSocket.Client.Utils;
using P2PSocket.Core.Utils;
using System.Linq;

namespace P2PSocket.Client.Commands
{
    [CommandFlag(Core.P2PCommandType.P2P0x0201)]
    public class Cmd_0x0201 : P2PCommand
    {
        readonly P2PTcpClient m_tcpClient;
        BinaryReader m_data { get; }
        public Cmd_0x0201(P2PTcpClient tcpClient, byte[] data)
        {
            m_tcpClient = tcpClient;
            m_data = new BinaryReader(new MemoryStream(data));
        }
        public override bool Excute()
        {
            int step = m_data.ReadInt32();
            switch (step)
            {
                case 2:
                    {
                        bool isDestClient = BinaryUtils.ReadBool(m_data);
                        string token = BinaryUtils.ReadString(m_data);
                        if (isDestClient) CreateTcpFromDest(token);
                        else CreateTcpFromSource(token);
                    }
                    break;
                case 4:
                    ListenPort();
                    break;
                case -1:
                    {
                        string message = BinaryUtils.ReadString(m_data);
                        LogUtils.Warning($"【P2P】穿透失败，错误消息：{Environment.NewLine}{message}");
                        m_tcpClient.Close();
                    }
                    break;
            }
            return true;
        }

        /// <summary>
        ///     从目标端创建与服务器的tcp连接
        /// </summary>
        /// <param name="token"></param>
        public void CreateTcpFromDest(string token)
        {
            try
            {
                Models.Send.Send_0x0201_Bind sendPacket = new Models.Send.Send_0x0201_Bind(token);
                int port = BinaryUtils.ReadInt(m_data);
                PortMapItem destMap = Global.PortMapList.FirstOrDefault(t => t.LocalPort == port && string.IsNullOrEmpty(t.LocalAddress));

                P2PTcpClient portClient = null;

                if (destMap != null)
                    if (destMap.MapType == PortMapType.ip)
                        portClient = new P2PTcpClient(destMap.RemoteAddress, destMap.RemotePort);
                    else
                        portClient = new P2PTcpClient("127.0.0.1", port);
                else
                    portClient = new P2PTcpClient("127.0.0.1", port);


                P2PTcpClient serverClient = new P2PTcpClient(Global.ServerAddress, Global.ServerPort);
                portClient.IsAuth = serverClient.IsAuth = true;
                portClient.ToClient = serverClient;
                serverClient.ToClient = portClient;
                serverClient.Client.Send(sendPacket.PackData());
                Global.TaskFactory.StartNew(() => { Global_Func.ListenTcp<ReceivePacket>(serverClient); });
            }
            catch (Exception ex)
            {
                LogUtils.Error($"【P2P】命令：0x0201 错误：{Environment.NewLine}{ex.ToString()}");
            }
        }

        /// <summary>
        ///     从发起端创建与服务器的tcp连接
        /// </summary>
        /// <param name="token"></param>
        public void CreateTcpFromSource(string token)
        {
            Models.Send.Send_0x0201_Bind sendPacket = new Models.Send.Send_0x0201_Bind(token);
            if (Global.WaiteConnetctTcp.ContainsKey(token))
            {
                P2PTcpClient portClient = Global.WaiteConnetctTcp[token];
                Global.WaiteConnetctTcp.Remove(token);
                P2PTcpClient serverClient = new P2PTcpClient(Global.ServerAddress, Global.ServerPort);
                portClient.IsAuth = serverClient.IsAuth = true;
                portClient.ToClient = serverClient;
                serverClient.ToClient = portClient;
                serverClient.Client.Send(sendPacket.PackData());
                Global.TaskFactory.StartNew(() => { Global_Func.ListenTcp<ReceivePacket>(serverClient); });
            }
            else
            {
                LogUtils.Warning($"【P2P】命令：0x0201 接收到P2P命令，但已超时.");
            }
        }

        /// <summary>
        ///     监听连接外部程序的端口
        /// </summary>
        public void ListenPort()
        {
            //  监听端口
            Global.TaskFactory.StartNew(() => { Global_Func.ListenTcp<Packet_0x0202>(m_tcpClient.ToClient); });
        }
    }
}
