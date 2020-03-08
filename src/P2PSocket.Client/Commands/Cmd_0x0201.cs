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
using System.Threading.Tasks;
using System.Net;
using System.Threading;

namespace P2PSocket.Client.Commands
{
    [CommandFlag(Core.P2PCommandType.P2P0x0201)]
    public class Cmd_0x0201 : P2PCommand
    {
        readonly P2PTcpClient m_tcpClient;
        BinaryReader data { get; }
        public Cmd_0x0201(P2PTcpClient tcpClient, byte[] data)
        {
            m_tcpClient = tcpClient;
            this.data = new BinaryReader(new MemoryStream(data));
        }
        public override bool Excute()
        {
            int step = data.ReadInt32();
            switch (step)
            {
                case 2:
                    {
                        bool isDestClient = BinaryUtils.ReadBool(data);
                        string token = BinaryUtils.ReadString(data);
                        int p2pType = BinaryUtils.ReadInt(data);
                        if (p2pType == 0)
                        {
                            if (isDestClient) CreateTcpFromDest(token);
                            else CreateTcpFromSource(token);
                        }
                        else
                        {

                            if (isDestClient) CreateTcpFromDest_DirectConnect(token);
                            else CreateTcpFromSource_DirectConnect(token);
                        }
                    }
                    break;
                case 4:
                    ListenPort();
                    break;
                case 14:
                    //TcpP2P
                    {
                        TryBindP2PTcp();
                    }
                    break;
                case -1:
                    {
                        string message = BinaryUtils.ReadString(data);
                        LogUtils.Warning($"内网穿透失败，错误消息：{Environment.NewLine}{message}");
                    }
                    break;
            }
            return true;
        }

        public void TryBindP2PTcp()
        {
            string ip = BinaryUtils.ReadString(data);
            int port = BinaryUtils.ReadInt(data);
            string token = BinaryUtils.ReadString(data);
            int maxTryCount = 1;
            bool isConnected = false;
            int bindPort = Convert.ToInt32(m_tcpClient.Client.LocalEndPoint.ToString().Split(':')[1]);
            Task.Factory.StartNew(() =>
            {
                P2PTcpClient p2pClient = new P2PTcpClient();
                p2pClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                p2pClient.Client.Bind(new IPEndPoint(IPAddress.Any, bindPort));
                while (maxTryCount > 0)
                {
                    maxTryCount--;
                    try
                    {
                        p2pClient.Connect(ip, port);
                        p2pClient.UpdateEndPoint();
                        isConnected = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        LogUtils.Debug($"P2P连接失败：{bindPort}\r\n{ex.ToString()}");
                        Thread.Sleep(100);
                    }

                }
                if (isConnected)
                {
                    LogUtils.Info($"命令：0x0201  内网穿透（P2P模式）连接成功 port:{bindPort} token:{token}");
                    P2PBind_DirectConnect(p2pClient, token);
                }
                else
                {
                    LogUtils.Info($"命令：0x0201  内网穿透（P2P模式）连接失败 port:{bindPort} token:{token}");
                    p2pClient.SafeClose();
                    if (TcpCenter.Instance.WaiteConnetctTcp.ContainsKey(token))
                    {
                        P2PTcpClient portClient = TcpCenter.Instance.WaiteConnetctTcp[token];
                        portClient.SafeClose();
                        TcpCenter.Instance.WaiteConnetctTcp.Remove(token);
                    }
                }
                m_tcpClient.SafeClose();
            });
        }

        public void P2PBind_DirectConnect(P2PTcpClient p2pClient, string token)
        {
            try
            {
                if (m_tcpClient.P2PLocalPort > 0)
                {
                    int port = m_tcpClient.P2PLocalPort;
                    PortMapItem destMap = ConfigCenter.Instance.PortMapList.FirstOrDefault(t => t.LocalPort == port && string.IsNullOrEmpty(t.LocalAddress));

                    P2PTcpClient portClient = null;

                    if (destMap != null)
                        if (destMap.MapType == PortMapType.ip)
                            portClient = new P2PTcpClient(destMap.RemoteAddress, destMap.RemotePort);
                        else
                            portClient = new P2PTcpClient("127.0.0.1", port);
                    else
                        portClient = new P2PTcpClient("127.0.0.1", port);


                    portClient.IsAuth = p2pClient.IsAuth = true;
                    portClient.ToClient = p2pClient;
                    p2pClient.ToClient = portClient;
                    AppCenter.Instance.StartNewTask(() => { Global_Func.BindTcp(p2pClient, portClient); });
                    AppCenter.Instance.StartNewTask(() => { Global_Func.BindTcp(portClient, p2pClient); });
                }
                else
                {
                    if (TcpCenter.Instance.WaiteConnetctTcp.ContainsKey(token))
                    {
                        P2PTcpClient portClient = TcpCenter.Instance.WaiteConnetctTcp[token];
                        TcpCenter.Instance.WaiteConnetctTcp.Remove(token);
                        portClient.IsAuth = p2pClient.IsAuth = true;
                        portClient.ToClient = p2pClient;
                        p2pClient.ToClient = portClient;
                        AppCenter.Instance.StartNewTask(() => { Global_Func.BindTcp(p2pClient, portClient); });
                        AppCenter.Instance.StartNewTask(() => { Global_Func.BindTcp(portClient, p2pClient); });
                    }
                    else
                    {
                        LogUtils.Warning($"命令：0x0201 接收到内网穿透命令，但已超时. token:{token}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogUtils.Error(ex.Message);
            }
        }

        public void CreateTcpFromDest_DirectConnect(string token)
        {
            try
            {
                int port = BinaryUtils.ReadInt(data);
                Models.Send.Send_0x0201_Bind sendPacket = new Models.Send.Send_0x0201_Bind(token);
                Utils.LogUtils.Info($"命令：0x0201  正尝试内网穿透(P2P模式) token:{token}");
                P2PTcpClient serverClient = new P2PTcpClient();
                serverClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                serverClient.Connect(ConfigCenter.Instance.ServerAddress, ConfigCenter.Instance.ServerPort);
                serverClient.IsAuth = true;
                serverClient.P2PLocalPort = port;
                serverClient.UpdateEndPoint();
                serverClient.Client.Send(sendPacket.PackData());
                AppCenter.Instance.StartNewTask(() => { Global_Func.ListenTcp<ReceivePacket>(serverClient); });
            }
            catch (Exception ex)
            {
                LogUtils.Error($"命令：0x0201 尝试内网穿透(P2P模式) 发生错误：{Environment.NewLine}{ex.Message}");
            }
        }
        public void CreateTcpFromSource_DirectConnect(string token)
        {
            Models.Send.Send_0x0201_Bind sendPacket = new Models.Send.Send_0x0201_Bind(token);
            Utils.LogUtils.Info($"命令：0x0201  正尝试内网穿透（P2P模式）token:{token}");
            if (TcpCenter.Instance.WaiteConnetctTcp.ContainsKey(token))
            {
                P2PTcpClient serverClient = new P2PTcpClient();
                serverClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                serverClient.Connect(ConfigCenter.Instance.ServerAddress, ConfigCenter.Instance.ServerPort);
                serverClient.IsAuth = true;
                serverClient.UpdateEndPoint();
                serverClient.Client.Send(sendPacket.PackData());
                AppCenter.Instance.StartNewTask(() => { Global_Func.ListenTcp<ReceivePacket>(serverClient); });
            }
            else
            {
                LogUtils.Warning($"命令：0x0201 接收到内网穿透（P2P模式）命令，但已超时. token:{token}");
            }
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
                Utils.LogUtils.Info($"命令：0x0201  正在绑定内网穿透（3端）通道 token:{token}");
                int port = BinaryUtils.ReadInt(data);
                PortMapItem destMap = ConfigCenter.Instance.PortMapList.FirstOrDefault(t => t.LocalPort == port && string.IsNullOrEmpty(t.LocalAddress));

                P2PTcpClient portClient = null;

                if (destMap != null)
                    if (destMap.MapType == PortMapType.ip)
                        portClient = new P2PTcpClient(destMap.RemoteAddress, destMap.RemotePort);
                    else
                        portClient = new P2PTcpClient("127.0.0.1", port);
                else
                    portClient = new P2PTcpClient("127.0.0.1", port);


                P2PTcpClient serverClient = new P2PTcpClient(ConfigCenter.Instance.ServerAddress, ConfigCenter.Instance.ServerPort);
                portClient.IsAuth = serverClient.IsAuth = true;
                portClient.ToClient = serverClient;
                serverClient.ToClient = portClient;
                serverClient.Client.Send(sendPacket.PackData());
                AppCenter.Instance.StartNewTask(() => { Global_Func.ListenTcp<ReceivePacket>(serverClient); });
            }
            catch (Exception ex)
            {
                LogUtils.Error($"命令：0x0201 绑定内网穿透（3端）通道错误：{Environment.NewLine}{ex.Message}");
            }
        }

        /// <summary>
        ///     从发起端创建与服务器的tcp连接
        /// </summary>
        /// <param name="token"></param>
        public void CreateTcpFromSource(string token)
        {
            Models.Send.Send_0x0201_Bind sendPacket = new Models.Send.Send_0x0201_Bind(token);
            Utils.LogUtils.Info($"命令：0x0201  正尝试内网穿透（转发模式）token:{token}");
            if (TcpCenter.Instance.WaiteConnetctTcp.ContainsKey(token))
            {
                P2PTcpClient portClient = TcpCenter.Instance.WaiteConnetctTcp[token];
                TcpCenter.Instance.WaiteConnetctTcp.Remove(token);
                P2PTcpClient serverClient = new P2PTcpClient(ConfigCenter.Instance.ServerAddress, ConfigCenter.Instance.ServerPort);
                portClient.IsAuth = serverClient.IsAuth = true;
                portClient.ToClient = serverClient;
                serverClient.ToClient = portClient;
                serverClient.Client.Send(sendPacket.PackData());
                AppCenter.Instance.StartNewTask(() => { Global_Func.ListenTcp<ReceivePacket>(serverClient); });
            }
            else
            {
                LogUtils.Warning($"命令：0x0201 接收到内网穿透（转发模式）命令，但已超时. token:{token}");
            }
        }

        /// <summary>
        ///     监听连接外部程序的端口
        /// </summary>
        public void ListenPort()
        {
            //  监听端口
            AppCenter.Instance.StartNewTask(() => { Global_Func.ListenTcp<Packet_0x0202>(m_tcpClient.ToClient); });
        }
    }
}
