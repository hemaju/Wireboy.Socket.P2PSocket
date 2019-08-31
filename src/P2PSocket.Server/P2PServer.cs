using P2PSocket.Core;
using P2PSocket.Core.Commands;
using P2PSocket.Core.Extends;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using P2PSocket.Server.Commands;
using P2PSocket.Server.Models;
using P2PSocket.Server.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace P2PSocket.Server
{
    public class P2PServer
    {
        public List<TcpListener> ListenerList { set; get; } = new List<TcpListener>();
        public P2PServer()
        {

        }

        /// <summary>
        ///     启动服务
        /// </summary>
        public void StartServer()
        {
            ListenMessagePort();
            StartPortMap();
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
                    if (item.MapType == PortMapType.ip)
                    {
                        ListenPortMapPortWithIp(item);
                    }
                    else
                    {
                        ListenPortMapPortWithServerName(item);
                    }
                });
            }
        }

        /// <summary>
        ///     监听消息端口
        /// </summary>
        private void ListenMessagePort()
        {
            try
            {
                TcpListener listener = new TcpListener(IPAddress.Any, Global.LocalPort);
                listener.Start();
                ListenerList.Add(listener);
                Global.TaskFactory.StartNew(() =>
                {
                    try
                    {
                        while (true)
                        {
                            Socket socket = listener.AcceptSocket();
                            P2PTcpClient tcpClient = new P2PTcpClient(socket);
                            //接收数据
                            Global.TaskFactory.StartNew(() =>
                        {
                            Global_Func.ListenTcp<ReceivePacket>(tcpClient);
                        });
                        }
                    }
                    catch (Exception ex)
                    {
                        LogUtils.Debug(ex.Message);
                    }
                });
            }
            catch
            {
                LogUtils.Error($"【失败】服务端口：{Global.LocalPort} 监听失败.");
            }
        }

        private void ListenPortMapPortWithServerName(PortMapItem item)
        {
            try
            {
                TcpListener listener = new TcpListener(IPAddress.Any, item.LocalPort);
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
                            if (Global.TcpMap.ContainsKey(item.RemoteAddress) && Global.TcpMap[item.RemoteAddress].TcpClient.Connected)
                            {
                                //加入待连接集合
                                Global.WaiteConnetctTcp.Add(token, tcpClient);
                                //发送p2p申请
                                Models.Send.Send_0x0211 packet = new Models.Send.Send_0x0211(token, item.RemotePort);
                                Global.TcpMap[item.RemoteAddress].TcpClient.Client.Send(packet.PackData());
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
                                LogUtils.Warning($"【失败】内网穿透：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort} 客户端不在线!");
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
                TcpListener listener = new TcpListener(IPAddress.Any, item.LocalPort);
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
                            catch (Exception ex)
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
