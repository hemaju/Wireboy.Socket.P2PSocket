using P2PSocket.Client.Models.Send;
using P2PSocket.Client.Utils;
using P2PSocket.Core;
using P2PSocket.Core.Extends;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace P2PSocket.Client
{
    public class P2PClient
    {
        public List<TcpListener> ListenerList { set; get; } = new List<TcpListener>();
        public P2PClient()
        {

        }

        public void StartServer()
        {
            //连接服务器
            ConnectServer();
            //启动端口映射
            StartPortMap();
            //断线重连功能
            Global.TaskFactory.StartNew(() => { TestAndReconnectServer(); });
        }

        private void ConnectServer()
        {
            try
            {
                P2PTcpClient p2PTcpClient = new P2PTcpClient(Global.ServerAddress, Global.ServerPort);
                LogUtils.Show($"{DateTime.Now.ToString("【成功】[HH:mm:ss]")}连接服务器:{Global.ServerAddress}:{Global.ServerPort}");
                p2PTcpClient.IsAuth = true;
                Global.P2PServerTcp = p2PTcpClient;
                //接收数据
                Global.TaskFactory.StartNew(() =>
                {
                    //向服务器发送客户端信息
                    InitServerInfo(p2PTcpClient);
                    //监听来自服务器的消息
                    Global_Func.ListenTcp<ReceivePacket>(p2PTcpClient);
                });
            }
            catch
            {
                LogUtils.Error($"{DateTime.Now.ToString("【失败】[HH:mm:ss]")}连接服务器:{Global.ServerAddress}:{Global.ServerPort}");
            }
        }

        public void TestAndReconnectServer()
        {
            while (true)
            {
                Thread.Sleep(5000);
                try
                {
                    if (Global.P2PServerTcp != null && Global.P2PServerTcp.Connected)
                    {
                        Send_0x0052 sendPacket = new Send_0x0052();
                        int count = Global.P2PServerTcp.Client.Send(sendPacket.PackData());
                    }
                    else
                    {
                        ConnectServer();
                    }
                }
                catch(Exception ex)
                {
                    LogUtils.Warning($"【断线重连】错误消息：{Environment.NewLine}{ex.ToString()}");
                    try
                    {
                        Global.P2PServerTcp.Close();
                    }
                    catch { }
                    Global.P2PServerTcp = null;
                    ConnectServer();
                }
            }
        }

        /// <summary>
        ///     向服务器发送客户端信息
        /// </summary>
        /// <param name="tcpClient"></param>
        private void InitServerInfo(P2PTcpClient tcpClient)
        {
            Send_0x0101 sendPacket = new Send_0x0101();
            int length = tcpClient.Client.Send(sendPacket.PackData());
        }


        /// <summary>
        ///     监听映射端口
        /// </summary>
        private void StartPortMap()
        {
            foreach (PortMapItem item in Global.PortMapList)
            {
                Global.TaskFactory.StartNew(() =>
                {
                    try
                    {
                        if (item.MapType == PortMapType.ip)
                        {
                            ListenPortMapPortWithIp(item);
                        }
                        else
                        {
                            ListenPortMapPortWithServerName(item);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogUtils.Error(ex.Message);
                    }
                });
            }
        }

        private void ListenPortMapPortWithServerName(PortMapItem item)
        {
            try
            {
                TcpListener listener = new TcpListener(string.IsNullOrEmpty(item.LocalAddress) ? IPAddress.Any : IPAddress.Parse(item.LocalAddress), item.LocalPort);
                listener.Start();
                ListenerList.Add(listener);
                LogUtils.Show($"【成功】端口映射：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}");
                Global.TaskFactory.StartNew(() =>
                {
                    while (true)
                    {
                        Socket socket = listener.AcceptSocket();
                        P2PTcpClient tcpClient = new P2PTcpClient(socket);
                        Global.TaskFactory.StartNew(() =>
                        {
                            string token = tcpClient.Token;
                            //获取目标tcp
                            if (Global.P2PServerTcp != null && Global.P2PServerTcp.Connected)
                            {
                                //加入待连接集合
                                Global.WaiteConnetctTcp.Add(token, tcpClient);
                                //发送p2p申请
                                Send_0x0201_Apply packet = new Send_0x0201_Apply(token, item.RemoteAddress, item.RemotePort);
                                Global.P2PServerTcp.Client.Send(packet.PackData());
                                //LogUtils.Debug("P2P第一步：向服务器发送申请.");
                                Global.TaskFactory.StartNew(() =>
                                {
                                    Thread.Sleep(Global.P2PTimeout);
                                        //如果5秒后没有匹配成功，则关闭连接
                                        if (Global.WaiteConnetctTcp.ContainsKey(token))
                                    {
                                        LogUtils.Warning($"【失败】内网穿透：{Global.P2PTimeout}秒无响应，已超时.");
                                        Global.WaiteConnetctTcp[token].Close();
                                        Global.WaiteConnetctTcp.Remove(token);
                                    }
                                });
                            }
                            else
                            {
                                LogUtils.Warning($"【失败】内网穿透：未连接服务器!");
                                tcpClient.Close();
                            }
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                LogUtils.Error($"【失败】端口映射：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}{Environment.NewLine}{ex.ToString()}");
            }
        }

        /// <summary>
        ///     直接转发类型的端口监听
        /// </summary>
        /// <param name="item"></param>
        private void ListenPortMapPortWithIp(PortMapItem item)
        {
            try
            {
                TcpListener listener = new TcpListener(string.IsNullOrEmpty(item.LocalAddress) ? IPAddress.Any : IPAddress.Parse(item.LocalAddress), item.LocalPort);
                listener.Start();
                ListenerList.Add(listener);
                LogUtils.Show($"【成功】端口映射：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}");
                Global.TaskFactory.StartNew(() =>
                {
                    while (true)
                    {
                        Socket socket = listener.AcceptSocket();
                        P2PTcpClient tcpClient = new P2PTcpClient(socket);
                        Global.TaskFactory.StartNew(() =>
                        {
                            try
                            {
                                P2PTcpClient ipClient = new P2PTcpClient(item.RemoteAddress, item.RemotePort);
                                tcpClient.ToClient = ipClient;
                                ipClient.ToClient = tcpClient;
                            }
                            catch(Exception ex)
                            {
                                tcpClient.Close();
                                LogUtils.Error($"【失败】内网穿透：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}{Environment.NewLine}{ex.ToString()}");
                            }
                            if (tcpClient.Connected)
                            {
                                Global.TaskFactory.StartNew(() => { ListenPortMapTcpWithIp(tcpClient); });
                                Global.TaskFactory.StartNew(() => { ListenPortMapTcpWithIp(tcpClient.ToClient); });
                            }
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                LogUtils.Error($"【失败】端口映射：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}{Environment.NewLine}{ex.ToString()}");
            }
        }

        /// <summary>
        ///     监听映射端口并转发数据（ip直接转发模式）
        /// </summary>
        /// <param name="readClient"></param>
        private void ListenPortMapTcpWithIp(P2PTcpClient readClient)
        {
            if (readClient.ToClient == null || !readClient.ToClient.Connected)
            {
                LogUtils.Warning($"【失败】IP数据转发：绑定的Tcp连接已断开.");
                readClient.Close();
                return;
            }
            byte[] buffer = new byte[P2PGlobal.P2PSocketBufferSize];
            NetworkStream readStream = readClient.GetStream();
            NetworkStream toStream = readClient.ToClient.GetStream();
            while (readClient.Connected)
            {
                int curReadLength = readStream.ReadSafe(buffer, 0, buffer.Length);
                if (curReadLength > 0)
                {
                    if (readClient.ToClient != null && readClient.ToClient.Connected)
                    {
                        toStream.Write(buffer, 0, curReadLength);
                    }
                    else
                    {
                        LogUtils.Warning($"【失败】IP数据转发：目标Tcp连接已断开.");
                        readClient.Close();
                        break;
                    }
                }
                else
                {
                    LogUtils.Warning($"【失败】IP数据转发：Tcp连接已断开.");
                    //如果tcp已关闭，需要关闭相关tcp
                    if (readClient.ToClient != null && readClient.ToClient.Connected)
                    {
                        readClient.ToClient.Close();
                    }
                    break;
                }
            }
        }
    }
}
