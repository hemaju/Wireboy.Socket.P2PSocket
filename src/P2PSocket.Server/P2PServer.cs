using P2PSocket.Core;
using P2PSocket.Core.Commands;
using P2PSocket.Core.Extends;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using P2PSocket.Server.Commands;
using P2PSocket.Server.Models;
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
            //服务端口限制在1000以上
            if (Global.LocalPort > 1000)
            {
                try
                {
                    TcpListener listener = new TcpListener(IPAddress.Any, Global.LocalPort);
                    listener.Start();
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
                                    Global_Func.ListenTcp<RecievePacket>(tcpClient);
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                    });
                }
                catch
                {
                    ConsoleUtils.WriteLine($"端口{Global.LocalPort}监听失败.");
                }
            }
        }

        private void ListenPortMapPortWithServerName(PortMapItem item)
        {
            //服务端口限制在1000以上
            if (item.LocalPort > 1000)
            {
                try
                {
                    TcpListener listener = new TcpListener(IPAddress.Any, item.LocalPort);
                    listener.Start();
                    ConsoleUtils.WriteLine($"Server:端口映射：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}");
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
                                if (Global.TcpMap.ContainsKey(item.RemoteAddress) && Global.TcpMap[item.RemoteAddress].Connected)
                                {
                                    //加入待连接集合
                                    Global.WaiteConnetctTcp.Add(token, tcpClient);
                                    //发送p2p申请
                                    Models.Send.Port2PApplyRequest packet = new Models.Send.Port2PApplyRequest(token, item.RemotePort);
                                    Debug.WriteLine("[服务器]发送Port2P连接申请");
                                    Global.TcpMap[item.RemoteAddress].Client.Send(packet.PackData());
                                    Global.TaskFactory.StartNew(() =>
                                    {
                                        Thread.Sleep(5000);
                                        //如果5秒后没有匹配成功，则关闭连接
                                        if (Global.WaiteConnetctTcp.ContainsKey(token))
                                        {
                                            Debug.WriteLine("[服务器]5秒无响应，关闭连接");
                                            Global.WaiteConnetctTcp[token].Close();
                                            Global.WaiteConnetctTcp.Remove(token);
                                        }
                                    });
                                }
                                else
                                {
                                    Debug.WriteLine($"[服务器][错误]端口映射:{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}服务不在线.");
                                    tcpClient.Close();
                                }
                            });
                        }
                    });
                }
                catch
                {
                    ConsoleUtils.WriteLine($"Server:端口映射{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}创建失败.");
                    throw new Exception($"端口映射{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}创建失败.");
                }
            }
            else
            {
                ConsoleUtils.WriteLine($"Server:端口必须大于1000,当前端口：{item.LocalPort}");
            }
        }

        /// <summary>
        ///     直接转发类型的端口监听
        /// </summary>
        /// <param name="item"></param>
        private void ListenPortMapPortWithIp(PortMapItem item)
        {
            //服务端口限制在1000以上
            if (item.LocalPort > 1000)
            {
                try
                {
                    TcpListener listener = new TcpListener(IPAddress.Any, item.LocalPort);
                    listener.Start();
                    ConsoleUtils.WriteLine($"Server:端口映射：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}");
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
                                catch
                                {
                                    tcpClient.Close();
                                    Debug.WriteLine($"端口{item.LocalPort}映射关闭,无法建立{item.RemoteAddress}:{item.RemotePort}tcp连接.");
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
                catch
                {
                    ConsoleUtils.WriteLine($"Server:端口映射{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}创建失败.");
                    throw new Exception($"端口映射{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}创建失败.");
                }
            }
            else
            {
                ConsoleUtils.WriteLine($"Server:端口必须大于1000,当前端口：{item.LocalPort}");
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
                Debug.WriteLine($"[错误]端口映射：目标tcp不存在");
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
                        Debug.WriteLine($"远程端口已关闭{readClient.Client.RemoteEndPoint}");
                        readClient.Close();
                        break;
                    }
                }
                else
                {
                    Debug.WriteLine($"从端口{readClient.LocalEndPoint}读取到0的数据");
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
