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
            Global.TaskFactory.StartNew(() => TestAndReconnectServer());
        }

        private void ConnectServer()
        {
            try
            {
                Global.P2PServerTcp = new P2PTcpClient(Global.ServerAddress, Global.ServerPort);
            }
            catch
            {
                Global.P2PServerTcp = null;
                LogUtils.Error($"{DateTime.Now.ToString("【失败】[HH:mm:ss]")}连接服务器:{Global.ServerAddress}:{Global.ServerPort}");
            }
            if (Global.P2PServerTcp != null)
            {
                LogUtils.Show($"{DateTime.Now.ToString("【成功】[HH:mm:ss]")}连接服务器:{Global.ServerAddress}:{Global.ServerPort}");
                Global.P2PServerTcp.IsAuth = true;
                //接收数据
                Global.TaskFactory.StartNew(() =>
                {
                    //向服务器发送客户端信息
                    InitServerInfo(Global.P2PServerTcp);
                    //监听来自服务器的消息
                    Global_Func.ListenTcp<ReceivePacket>(Global.P2PServerTcp);
                });
            }
        }

        public void TestAndReconnectServer()
        {
            //  使用双循环，将try...catch放在内层循环外侧，提高运行效率
            while (true)
            {
                try
                {
                    while (true)
                    {
                        Thread.Sleep(5000);
                        if (Global.P2PServerTcp != null) Global.P2PServerTcp.Client.Send(new Send_0x0052().PackData());
                        else ConnectServer();
                    }
                }
                catch (Exception ex)
                {
                    LogUtils.Warning($"【断线重连】错误消息：{Environment.NewLine}{ex.ToString()}");
                    Global.P2PServerTcp = null;
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
            tcpClient.Client.Send(sendPacket.PackData());
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
            TcpListener listener = new TcpListener(string.IsNullOrEmpty(item.LocalAddress) ? IPAddress.Any : IPAddress.Parse(item.LocalAddress), item.LocalPort);
            try
            {
                listener.Start();
            }
            catch (SocketException ex)
            {
                LogUtils.Error($"【失败】端口映射：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}{Environment.NewLine}{ex}");
            }
            ListenerList.Add(listener);
            LogUtils.Show($"【成功】端口映射：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}");
            Global.TaskFactory.StartNew(() =>
            {
                while (true)
                {
                    Socket socket = listener.AcceptSocket();
                    //获取目标tcp
                    if (Global.P2PServerTcp != null && Global.P2PServerTcp.Connected)
                    {
                        Global.TaskFactory.StartNew(() =>
                        {
                            P2PTcpClient tcpClient = new P2PTcpClient(socket);
                            //加入待连接集合
                            Global.WaiteConnetctTcp.Add(tcpClient.Token, tcpClient);
                            //发送p2p申请
                            Send_0x0201_Apply packet = new Send_0x0201_Apply(tcpClient.Token, item.RemoteAddress, item.RemotePort);
                            try
                            {
                                Global.P2PServerTcp.Client.Send(packet.PackData());
                            }
                            finally
                            {
                                //如果5秒后没有匹配成功，则关闭连接
                                Thread.Sleep(Global.P2PTimeout);
                                if (Global.WaiteConnetctTcp.ContainsKey(tcpClient.Token))
                                {
                                    LogUtils.Warning($"【失败】内网穿透：{Global.P2PTimeout}秒无响应，已超时.");
                                    Global.WaiteConnetctTcp[tcpClient.Token].Close();
                                    Global.WaiteConnetctTcp.Remove(tcpClient.Token);
                                }

                            }
                        });
                    }
                    else
                    {
                        LogUtils.Warning($"【失败】内网穿透：未连接服务器!");
                        socket.Close();
                    }
                }
            });
        }

        /// <summary>
        ///     直接转发类型的端口监听
        /// </summary>
        /// <param name="item"></param>
        private void ListenPortMapPortWithIp(PortMapItem item)
        {
            TcpListener listener = new TcpListener(string.IsNullOrEmpty(item.LocalAddress) ? IPAddress.Any : IPAddress.Parse(item.LocalAddress), item.LocalPort);
            try
            {
                listener.Start();
            }
            catch (Exception ex)
            {
                LogUtils.Error($"【失败】端口映射：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}{Environment.NewLine}{ex.ToString()}");
            }
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
                        P2PTcpClient ipClient = null;
                        try
                        {
                            ipClient = new P2PTcpClient(item.RemoteAddress, item.RemotePort);
                        }
                        catch (Exception ex)
                        {
                            tcpClient.Close();
                            LogUtils.Error($"【失败】内网穿透：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}{Environment.NewLine}{ex}");
                        }
                        if (ipClient.Connected)
                        {
                            tcpClient.ToClient = ipClient;
                            ipClient.ToClient = tcpClient;
                            Global.TaskFactory.StartNew(() => ListenPortMapTcpWithIp(tcpClient));
                            Global.TaskFactory.StartNew(() => ListenPortMapTcpWithIp(tcpClient.ToClient));
                        }
                    });
                }
            });
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
            try
            {
                NetworkStream readStream = readClient.GetStream();
                NetworkStream toStream = readClient.ToClient.GetStream();
                while (readClient.Connected)
                {
                    int curReadLength = readStream.ReadSafe(buffer, 0, buffer.Length);
                    if (curReadLength > 0)
                    {
                        toStream.Write(buffer, 0, curReadLength);
                    }
                    else
                    {
                        LogUtils.Warning($"【失败】IP数据转发：Tcp连接已断开.");
                        //如果tcp已关闭，需要关闭相关tcp
                        try
                        {
                            readClient.ToClient.Close();
                        }
                        finally
                        {
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogUtils.Warning($"【失败】IP数据转发：目标Tcp连接已断开.");
                readClient.Close();
            }
        }
    }
}
