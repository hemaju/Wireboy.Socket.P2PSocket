using P2PSocket.Client.Models.Send;
using P2PSocket.Client.Utils;
using P2PSocket.Core;
using P2PSocket.Core.Extends;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace P2PSocket.Client
{
    public class P2PClient
    {
        public P2PClient()
        {

        }

        internal void ConnectServer()
        {
            try
            {
                TcpCenter.Instance.P2PServerTcp = new P2PTcpClient(ConfigCenter.Instance.ServerAddress, ConfigCenter.Instance.ServerPort);
            }
            catch
            {
                LogUtils.Error($"{DateTime.Now.ToString("HH:mm:ss")} 无法连接服务器:{ConfigCenter.Instance.ServerAddress}:{ConfigCenter.Instance.ServerPort}");
                return;
            }
            LogUtils.Info($"{DateTime.Now.ToString("HH:mm:ss")} 已连接服务器:{ConfigCenter.Instance.ServerAddress}:{ConfigCenter.Instance.ServerPort}", false);
            TcpCenter.Instance.P2PServerTcp.IsAuth = true;
            AppCenter.Instance.StartNewTask(() =>
            {
                //向服务器发送客户端信息
                InitServerInfo(TcpCenter.Instance.P2PServerTcp);
                //监听来自服务器的消息
                Global_Func.ListenTcp<ReceivePacket>(TcpCenter.Instance.P2PServerTcp);
            });
        }

        internal void TestAndReconnectServer()
        {
            Guid curGuid = AppCenter.Instance.CurrentGuid;
            while (true)
            {
                Thread.Sleep(5000);
                if (curGuid != AppCenter.Instance.CurrentGuid) break;
                if (TcpCenter.Instance.P2PServerTcp != null)
                {
                    try
                    {
                        TcpCenter.Instance.P2PServerTcp.Client.Send(new Send_0x0052().PackData());
                    }
                    catch (Exception ex)
                    {
                        LogUtils.Warning($"{DateTime.Now.ToString("HH:mm:ss")} 服务器连接已被断开");
                        TcpCenter.Instance.P2PServerTcp = null;
                    }
                }
                else
                {
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
            LogUtils.Info($"客户端名称：{ConfigCenter.Instance.ClientName}");
            tcpClient.Client.Send(sendPacket.PackData());
        }


        /// <summary>
        ///     监听映射端口
        /// </summary>
        internal void StartPortMap()
        {
            if (ConfigCenter.Instance.PortMapList.Count == 0) return;
            Dictionary<string, TcpListener> curListenerList = TcpCenter.Instance.ListenerList;
            TcpCenter.Instance.ListenerList = new Dictionary<string, TcpListener>();
            foreach (PortMapItem item in ConfigCenter.Instance.PortMapList)
            {
                string key = $"{item.LocalAddress}:{item.LocalPort}";
                if (curListenerList.ContainsKey(key))
                {
                    LogUtils.Trace($"正在监听端口：{key}");
                    TcpCenter.Instance.ListenerList.Add(key, curListenerList[key]);
                    curListenerList.Remove(key);
                    continue;
                }
                if (item.MapType == PortMapType.ip)
                {
                    ListenPortMapPortWithIp(item);
                }
                else
                {
                    ListenPortMapPortWithServerName(item);
                }
            }
            foreach (TcpListener listener in curListenerList.Values)
            {
                LogUtils.Trace($"停止端口监听：{listener.LocalEndpoint.ToString()}");
                listener.Stop();
            }
        }

        private void ListenPortMapPortWithServerName(PortMapItem item)
        {
            TcpListener listener = null;
            try
            {
                listener = new TcpListener(string.IsNullOrEmpty(item.LocalAddress) ? IPAddress.Any : IPAddress.Parse(item.LocalAddress), item.LocalPort);
                listener.Start();
            }
            catch (SocketException ex)
            {
                LogUtils.Error($"端口映射失败：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}{Environment.NewLine}{ex.Message}");
                return;
            }
            TcpCenter.Instance.ListenerList.Add($"{item.LocalAddress}:{item.LocalPort}", listener);
            LogUtils.Info($"端口映射：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}", false);
            AppCenter.Instance.StartNewTask(() =>
            {
                while (true)
                {
                    Socket socket = null;
                    try
                    {
                        socket = listener.AcceptSocket();
                    }
                    catch
                    {
                        break;
                    }
                    //获取目标tcp
                    if (TcpCenter.Instance.P2PServerTcp != null && TcpCenter.Instance.P2PServerTcp.Connected)
                    {
                        AppCenter.Instance.StartNewTask(() =>
                        {
                            P2PTcpClient tcpClient = new P2PTcpClient(socket);
                            //加入待连接集合
                            TcpCenter.Instance.WaiteConnetctTcp.Add(tcpClient.Token, tcpClient);
                            //发送p2p申请
                            Send_0x0201_Apply packet = new Send_0x0201_Apply(tcpClient.Token, item.RemoteAddress, item.RemotePort, item.P2PType);
                            if (item.P2PType == 0)
                            {
                                LogUtils.Info($"建立内网穿透（转发模式）通道 token:{tcpClient.Token} client:{item.RemoteAddress} port:{item.RemotePort}");
                            }
                            else
                            {
                                LogUtils.Info($"建立内网穿透（P2P模式）通道 token:{tcpClient.Token} client:{item.RemoteAddress} port:{item.RemotePort}");
                            }
                            try
                            {
                                TcpCenter.Instance.P2PServerTcp.Client.Send(packet.PackData());
                            }
                            finally
                            {
                                //如果5秒后没有匹配成功，则关闭连接
                                Thread.Sleep(ConfigCenter.P2PTimeout);
                                if (TcpCenter.Instance.WaiteConnetctTcp.ContainsKey(tcpClient.Token))
                                {
                                    LogUtils.Warning($"内网穿透失败：token:{tcpClient.Token} {item.LocalPort}->{item.RemoteAddress}:{item.RemotePort} {ConfigCenter.P2PTimeout / 1000}秒无响应，已超时.");
                                    TcpCenter.Instance.WaiteConnetctTcp[tcpClient.Token].SafeClose();
                                    TcpCenter.Instance.WaiteConnetctTcp.Remove(tcpClient.Token);
                                }

                            }
                        });
                    }
                    else
                    {
                        LogUtils.Warning($"内网穿透失败：未连接到服务器!");
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
            TcpListener listener = null;
            try
            {
                listener = new TcpListener(string.IsNullOrEmpty(item.LocalAddress) ? IPAddress.Any : IPAddress.Parse(item.LocalAddress), item.LocalPort);
                listener.Start();
            }
            catch (Exception ex)
            {
                LogUtils.Error($"添加端口映射失败：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}{Environment.NewLine}{ex.ToString()}");
                return;
            }
            TcpCenter.Instance.ListenerList.Add($"{item.LocalAddress}:{item.LocalPort}", listener);
            LogUtils.Info($"添加端口映射成功：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}", false);
            AppCenter.Instance.StartNewTask(() =>
            {
                while (true)
                {
                    Socket socket = listener.AcceptSocket();
                    P2PTcpClient tcpClient = new P2PTcpClient(socket);
                    AppCenter.Instance.StartNewTask(() =>
                    {
                        P2PTcpClient ipClient = null;
                        try
                        {
                            ipClient = new P2PTcpClient(item.RemoteAddress, item.RemotePort);
                        }
                        catch (Exception ex)
                        {
                            tcpClient.SafeClose();
                            LogUtils.Error($"内网穿透失败：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}{Environment.NewLine}{ex}");
                        }
                        if (ipClient.Connected)
                        {
                            tcpClient.ToClient = ipClient;
                            ipClient.ToClient = tcpClient;
                            AppCenter.Instance.StartNewTask(() => ListenPortMapTcpWithIp(tcpClient));
                            AppCenter.Instance.StartNewTask(() => ListenPortMapTcpWithIp(tcpClient.ToClient));
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
                LogUtils.Warning($"数据转发（ip模式）失败：绑定的Tcp连接已断开.");
                readClient.SafeClose();
                return;
            }
            TcpCenter.Instance.ConnectedTcpList.Add(readClient);
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
                        LogUtils.Warning($"端口映射转发（ip模式）：源Tcp连接已断开.");
                        //如果tcp已关闭，需要关闭相关tcp
                        try
                        {
                            readClient.ToClient.SafeClose();
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
                LogUtils.Warning($"端口映射转发（ip模式）：目标Tcp连接已断开.");
                readClient.SafeClose();
            }
            TcpCenter.Instance.ConnectedTcpList.Remove(readClient);
        }
    }
}
