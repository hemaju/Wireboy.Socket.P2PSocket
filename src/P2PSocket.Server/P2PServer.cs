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
            foreach (PortMapItem item in ConfigCenter.Instance.PortMapList)
            {
                AppCenter.Instance.StartNewTask(() =>
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
            TcpListener listener = new TcpListener(IPAddress.Any, ConfigCenter.Instance.LocalPort);
            try
            {
                listener.Start();
                LogUtils.Info($"【成功】启动服务，端口：{ConfigCenter.Instance.LocalPort}");
            }
            catch
            {
                LogUtils.Error($"【失败】服务端口：{ConfigCenter.Instance.LocalPort} 监听失败.");
                return;
            }
            ListenerList.Add(listener);
            AppCenter.Instance.StartNewTask(() =>
            {
                try
                {
                    while (true)
                    {
                        Socket socket = listener.AcceptSocket();
                        P2PTcpClient tcpClient = new P2PTcpClient(socket);
                        LogUtils.Info($"端口{ ConfigCenter.Instance.LocalPort}新连入Tcp：{tcpClient.Client.RemoteEndPoint.ToString()}");
                        //接收数据
                        AppCenter.Instance.StartNewTask(() => Global_Func.ListenTcp<ReceivePacket>(tcpClient));
                    }
                }
                catch (Exception ex)
                {
                    LogUtils.Debug(ex.Message);
                }
            });
        }

        private void ListenPortMapPortWithServerName(PortMapItem item)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, item.LocalPort);
            try
            {
                listener.Start();
            }
            catch (Exception ex)
            {
                LogUtils.Error($"【失败】端口映射：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}{Environment.NewLine}{ex.ToString()}");
                return;
            }
            ListenerList.Add(listener);
            LogUtils.Info($"【成功】端口映射：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}", false);
            AppCenter.Instance.StartNewTask(() =>
            {
                TcpManage tcpManage = null;
                if (item.RemotePort == 3389)
                {
                    tcpManage = new TcpManage(6);
                }
                while (true)
                {
                    Socket socket = listener.AcceptSocket();
                    string remoteAddress = ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();
                    if (tcpManage != null)
                    {
                        tcpManage.AddTcp(remoteAddress);
                        if (!tcpManage.IsAllowConnect(remoteAddress))
                        {
                            LogUtils.Info($"【安全策略】阻止内网穿透：{remoteAddress}->{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}", false);
                            socket.SafeClose();
                            continue;
                        }
                    }
                    LogUtils.Info($"开始内网穿透：{remoteAddress}->{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}", false);
                    P2PTcpClient tcpClient = new P2PTcpClient(socket);
                    AppCenter.Instance.StartNewTask(() =>
                    {
                        string token = tcpClient.Token;
                        //获取目标tcp
                        if (ClientCenter.Instance.TcpMap.ContainsKey(item.RemoteAddress) && ClientCenter.Instance.TcpMap[item.RemoteAddress].TcpClient.Connected)
                        {
                            //加入待连接集合
                            ClientCenter.Instance.WaiteConnetctTcp.Add(token, tcpClient);
                            //发送p2p申请
                            Models.Send.Send_0x0211 packet = new Models.Send.Send_0x0211(token, item.RemotePort, tcpClient.RemoteEndPoint);
                            ClientCenter.Instance.TcpMap[item.RemoteAddress].TcpClient.Client.Send(packet.PackData());
                            AppCenter.Instance.StartNewTask(() =>
                            {
                                Thread.Sleep(ConfigCenter.Instance.P2PTimeout);
                                //如果5秒后没有匹配成功，则关闭连接
                                if (ClientCenter.Instance.WaiteConnetctTcp.ContainsKey(token))
                                {
                                    LogUtils.Warning($"【失败】内网穿透：{ConfigCenter.Instance.P2PTimeout / 1000}秒无响应，已超时.");
                                    ClientCenter.Instance.WaiteConnetctTcp[token].SafeClose();
                                    ClientCenter.Instance.WaiteConnetctTcp.Remove(token);
                                }
                            });
                        }
                        else
                        {
                            LogUtils.Warning($"【失败】内网穿透：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort} 客户端不在线!");
                            tcpClient.SafeClose();
                        }
                    });
                }
            });
        }

        /// <summary>
        ///     直接转发类型的端口监听
        /// </summary>
        /// <param name="item"></param>
        private void ListenPortMapPortWithIp(PortMapItem item)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, item.LocalPort);
            try
            {
                listener.Start();
            }
            catch (Exception ex)
            {
                LogUtils.Error($"【失败】端口映射：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}{Environment.NewLine}{ex.ToString()}");
                return;
            }
            ListenerList.Add(listener);
            LogUtils.Info($"【成功】端口映射：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}", false);
            AppCenter.Instance.StartNewTask(() =>
            {
                while (true)
                {
                    Socket socket = listener.AcceptSocket();
                    P2PTcpClient tcpClient = new P2PTcpClient(socket);
                    AppCenter.Instance.StartNewTask(() =>
                    {
                        try
                        {
                            P2PTcpClient ipClient = new P2PTcpClient(item.RemoteAddress, item.RemotePort);
                            tcpClient.ToClient = ipClient;
                            ipClient.ToClient = tcpClient;
                        }
                        catch (Exception ex)
                        {
                            tcpClient.SafeClose();
                            LogUtils.Error($"【失败】内网穿透：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}{Environment.NewLine}{ex.ToString()}");
                        }
                        if (tcpClient.Connected)
                        {
                            AppCenter.Instance.StartNewTask(() => { ListenPortMapTcpWithIp(tcpClient); });
                            AppCenter.Instance.StartNewTask(() => { ListenPortMapTcpWithIp(tcpClient.ToClient); });
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
                readClient.SafeClose();
                return;
            }
            byte[] buffer = new byte[P2PGlobal.P2PSocketBufferSize];
            NetworkStream readStream = readClient.GetStream();
            NetworkStream toStream = readClient.ToClient.GetStream();
            try
            {
                while (readClient.Connected)
                {
                    int curReadLength = readStream.ReadSafe(buffer, 0, buffer.Length);
                    if (curReadLength > 0)
                    {
                        if (readClient.ToClient != null)
                        {
                            toStream.Write(buffer, 0, curReadLength);
                        }
                        else
                        {
                            LogUtils.Warning($"【失败】IP数据转发：目标Tcp连接已释放.");
                            readClient.SafeClose();
                            break;
                        }
                    }
                    else
                    {
                        LogUtils.Warning($"【失败】IP数据转发：Tcp连接已断开.");
                        //如果tcp已关闭，需要关闭相关tcp
                        try
                        {
                            readClient.ToClient?.SafeClose();
                        }
                        finally { }
                        break;
                    }
                }
            }
            finally
            {
                LogUtils.Warning($"【失败】IP数据转发：目标Tcp连接已断开.");
                readClient.SafeClose();
            }
        }
    }
}
