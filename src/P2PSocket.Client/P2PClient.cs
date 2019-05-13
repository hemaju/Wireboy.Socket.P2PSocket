using P2PSocket.Client.Models.Send;
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
            Global.TaskFactory.StartNew(()=> { TestAndReconnectServer(); });
        }

        private void ConnectServer()
        {
            try
            {
                P2PTcpClient p2PTcpClient = new P2PTcpClient(Global.ServerAddress, Global.ServerPort);
                ConsoleUtils.WriteLine($"{DateTime.Now.ToString("[HH:mm:ss]")}服务器{Global.ServerAddress}:{Global.ServerPort}连接成功.");
                p2PTcpClient.IsAuth = true;
                Global.P2PServerTcp = p2PTcpClient;
                //接收数据
                Global.TaskFactory.StartNew(() =>
                {
                    //向服务器发送客户端信息
                    InitServerInfo(p2PTcpClient);
                    //监听来自服务器的消息
                    Global_Func.ListenTcp<RecievePacket>(p2PTcpClient);
                });
            }
            catch
            {
                ConsoleUtils.WriteLine($"服务器{Global.ServerAddress}:{Global.ServerPort}连接失败！");
            }
        }

        public void TestAndReconnectServer()
        {
            while (true)
            {
                Thread.Sleep(5000);
                if (Global.P2PServerTcp == null || !Global.P2PServerTcp.Connected)
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
            LoginRequest sendPacket = new LoginRequest();
            int length = tcpClient.Client.Send(sendPacket.PackData());
            Debug.WriteLine($"向服务器发送数据，长度{length}");
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

        private void ListenPortMapPortWithServerName(PortMapItem item)
        {
            //服务端口限制在1000以上
            if (item.LocalPort > 1000)
            {
                try
                {
                    TcpListener listener = new TcpListener(IPAddress.Any, item.LocalPort);
                    listener.Start();
                    ConsoleUtils.WriteLine($"Client:端口映射：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}");
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
                                    P2PApplyRequest packet = new P2PApplyRequest(token, item.RemoteAddress, item.RemotePort);
                                    Debug.WriteLine("P2P第一步：向服务器发送申请.");
                                    Global.P2PServerTcp.Client.Send(packet.PackData());

                                    Global.TaskFactory.StartNew(() =>
                                    {
                                        Thread.Sleep(5000);
                                        //如果5秒后没有匹配成功，则关闭连接
                                        if (Global.WaiteConnetctTcp.ContainsKey(token))
                                        {
                                            Debug.WriteLine("P2P第一步：5秒无响应，关闭连接");
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
                    ConsoleUtils.WriteLine($"Client:端口映射{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}创建失败.");
                    throw new Exception($"端口映射{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}创建失败.");
                }
            }
            else
            {
                ConsoleUtils.WriteLine($"Client:端口必须大于1000,当前端口：{item.LocalPort}");
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
                    ConsoleUtils.WriteLine($"Client:端口映射：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}");
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
                    ConsoleUtils.WriteLine($"Client:端口映射{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}创建失败.");
                    throw new Exception($"端口映射{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}创建失败.");
                }
            }
            else
            {
                ConsoleUtils.WriteLine($"端口必须大于1000,当前端口：{item.LocalPort}");
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
                        Debug.WriteLine($"远程端口已关闭{readClient.RemoteEndPoint}");
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
