using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using Wireboy.Socket.P2PClient.Models;
using System.Collections.Concurrent;

namespace Wireboy.Socket.P2PClient
{
    public class P2PService
    {
        /// <summary>
        /// 仅用于线程创建
        /// </summary>
        TaskFactory _taskFactory = new TaskFactory();
        /// <summary>
        /// 本地Tcp连接
        /// </summary>
        TcpClient _homeServerTcp = null;
        object _lockHomeServerTcp = new object();

        /// <summary>
        /// Home服务Tcp连接（注：不存在则创建，不应使用此属性判断null）
        /// </summary>
        TcpClient HomeServerTcp
        {
            set { _homeServerTcp = value; }
            get
            {
                if ((_homeServerTcp == null || !_homeServerTcp.Connected) && IsEnableHome)
                    lock (_lockHomeServerTcp)
                    {
                        try
                        {
                            if (_homeServerTcp == null || !_homeServerTcp.Connected) _homeServerTcp = new TcpClient("127.0.0.1", ConfigServer.AppSettings.LocalHomePort);
                            _taskFactory.StartNew(() =>
                            {
                                try
                                {
                                    ListenHomeServerPort();
                                }
                                catch (Exception ex)
                                {
                                    Logger.Write("监听Home服务端口异常：\r\n{0}", ex);
                                    BreakHomeServerTcp();
                                    BreakRemoteTcp();
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            Logger.Write("连接本地服务失败：\r\n{0}", ex);
                            BreakHomeServerTcp();
                            BreakRemoteTcp();
                        }
                    }
                return _homeServerTcp;
            }
        }
        /// <summary>
        /// 本地Client服务Tcp
        /// </summary>
        TcpClient ClientServerTcp { get; set; } = null;
        /// <summary>
        /// 服务器Tcp连接
        /// </summary>
        TcpClient ServerTcp { set; get; } = null;
        /// <summary>
        /// 是否启用Home服务
        /// </summary>
        public bool IsEnableHome { set; get; } = false;
        /// <summary>
        /// 是否启用Client服务
        /// </summary>
        public bool IsEnableClient { set; get; } = false;
        /// <summary>
        /// 远程Home服务名称
        /// </summary>
        private string ClientServerName { set; get; }


        public P2PService()
        {
        }

        /// <summary>
        /// 连接服务器
        /// </summary>
        public void ConnectServer()
        {

            if (ServerTcp == null)
            {
                _taskFactory.StartNew(() =>
                {
                    while (true)
                    {
                        if (ServerTcp == null)
                        {
                            try
                            {
                                Logger.Write("连接服务器{0}:{1}", ConfigServer.AppSettings.ServerIp, ConfigServer.AppSettings.ServerPort);
                                //连接服务器
                                ServerTcp = new TcpClient(ConfigServer.AppSettings.ServerIp, ConfigServer.AppSettings.ServerPort);
                                //重启Home服务
                                RestartLocalServer();
                                _taskFactory.StartNew(() =>
                                {
                                    Logger.Write("监听来自服务器的消息...");
                                    try
                                    {
                                        RecieveServerTcp();
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Write("监听服务器端口异常：\r\n{0}", ex);
                                        BreakHomeServerTcp();
                                        BreakClientServerTcp();
                                    }
                                });
                            }
                            catch (Exception ex)
                            {
                                Logger.Write("服务器连接异常:\r\n{0}", ex);
                                try
                                {
                                    //尝试关闭TCP连接
                                    ServerTcp.Close();
                                }
                                catch { }
                                ServerTcp = null;
                            }
                        }
                        else
                        {
                            try
                            {
                                SendHeartPackage();
                            }
                            catch (Exception ex)
                            {
                                Logger.Write("服务器连接异常:\r\n{0}", ex);
                                try
                                {
                                    //尝试关闭TCP连接
                                    ServerTcp.Close();
                                }
                                catch { }
                                ServerTcp = null;
                            }
                        }
                        //每2秒检测一次是否因为异常导致TCP关闭
                        Thread.Sleep(2000);
                    }
                });
            }
        }
        /// <summary>
        /// 启动Home服务
        /// </summary>
        /// <param name="homeName"></param>
        public void StartHomeServer(string homeName)
        {
            IsEnableHome = true;
            ConfigServer.AppSettings.HomeServerName = homeName;
            if (ServerTcp != null)
            {
                try
                {
                    //发送Home服务名称
                    ServerTcp.WriteAsync(homeName, MsgType.本地服务名);
                }
                catch (Exception ex)
                {
                    Logger.Write("启动Home服务异常：\r\n{0}", ex);
                    BreakHomeServerTcp();
                    BreakRemoteTcp();
                }
            }

        }

        public void RestartLocalServer()
        {
            if (IsEnableHome)
            {
                Thread.Sleep(500);
                //发送本地Home服务名称
                ServerTcp.WriteAsync(ConfigServer.AppSettings.HomeServerName, MsgType.本地服务名);
                Logger.Write("重启Home服务，服务名：{0}", ConfigServer.AppSettings.HomeServerName);
            }
            if (IsEnableClient)
            {
                Thread.Sleep(500);
                //发送远程Home服务名称
                ServerTcp.WriteAsync(ClientServerName, MsgType.远程服务名);
                Logger.Write("重启Client服务，服务名：{0}", ClientServerName);
            }
        }
        public void StartClientServer(string homeName)
        {
            ClientServerName = homeName;
            IsEnableClient = true;
            if (ServerTcp != null)
            {
                ServerTcp.WriteAsync(homeName, MsgType.远程服务名);
            }
            _taskFactory.StartNew(() =>
                {
                    try
                    {
                        Logger.Write("监听Client服务端口：{0}", ConfigServer.AppSettings.LocalClientPort);
                        TcpListener tcpListener = new TcpListener(IPAddress.Any, ConfigServer.AppSettings.LocalClientPort);
                        tcpListener.Start();
                        while (IsEnableClient)
                        {
                            TcpClient tcpClient = tcpListener.AcceptTcpClient();
                            Logger.Write("Client服务新接入tcp：{0}", tcpClient.ToString());
                            if (ClientServerTcp == null)
                            {
                                ClientServerTcp = tcpClient;
                                _taskFactory.StartNew(() =>
                                {
                                    Logger.Write("监听新联入Client服务的Tcp - {0}", tcpClient.Client.RemoteEndPoint);
                                    ListenClientServerPort();
                                });
                            }
                        }
                        Logger.Write("监听Client服务端口结束");
                    }
                    catch (Exception ex)
                    {
                        Logger.Write("监听Client服务端口异常：\r\n{0}", ex);
                        BreakClientServerTcp();
                        BreakRemoteTcp();
                    }
                });
        }

        /// <summary>
        /// 接收服务器数据
        /// </summary>
        public void RecieveServerTcp()
        {
            try
            {
                NetworkStream readStream = ServerTcp.GetStream();
                TcpHelper tcpHelper = new TcpHelper();
                byte[] buffer = new byte[10240];
                while (true)
                {
                    int length = readStream.Read(buffer, 0, buffer.Length);
                    ConcurrentQueue<byte[]> results = tcpHelper.RecieveTcp(buffer, length);
                    while (!results.IsEmpty)
                    {
                        byte[] data;
                        if (results.TryDequeue(out data))
                        {
                            ReievedServiceTcpCallBack(data, ServerTcp);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                BreakHomeServerTcp();
                BreakClientServerTcp();
                Logger.Write("接收服务器数据异常：\r\n{0}", ex);
            }
        }

        /// <summary>
        /// 接收一个完整数据包回调方法
        /// </summary>
        /// <param name="data">完整的数据包</param>
        /// <param name="tcpResult"></param>
        //public void ReievedServiceTcpCallBack(byte[] data, TcpResult tcpResult)
        public void ReievedServiceTcpCallBack(byte[] data, TcpClient tcpClient)
        {
            switch (data[0])
            {
                case (byte)MsgType.心跳包:
                    ; break;
                case (byte)MsgType.身份验证:
                    ; break;
                case (byte)MsgType.转发FromClient:
                    {
                        if (IsEnableHome)
                        {
                            try
                            {
                                Logger.Debug("转发数据到Home服务");
                                HomeServerTcp.WriteAsync(data.Skip(1).ToArray(), MsgType.不封包);
                            }
                            catch (Exception ex)
                            {
                                BreakHomeServerTcp();
                                BreakRemoteTcp();
                                Logger.Write("向本地端口发送数据错误：\r\n{0}", ex);
                            }
                        }
                        else
                        {
                            Logger.Debug("转发数据到Home服务失败，没有连接到Home服务的TCP");
                        }
                    }
                    break;
                case (byte)MsgType.转发FromHome:
                    {
                        if (ClientServerTcp != null)
                        {
                            try
                            {
                                Logger.Debug("转发数据到Client服务");
                                ClientServerTcp.WriteAsync(data.Skip(1).ToArray(), MsgType.不封包);
                            }
                            catch (Exception ex)
                            {
                                BreakClientServerTcp();
                                BreakRemoteTcp();
                                Logger.Write("向本地端口发送数据错误：\r\n{0}", ex);
                            }
                        }
                        else
                        {
                            Logger.Debug("转发数据到Client服务失败，没有连接到Client服务的TCP");
                        }
                    }
                    break;
                case (byte)MsgType.连接断开:
                    {
                        BreakHomeServerTcp();
                        BreakClientServerTcp();
                    }
                    break;
                case (byte)MsgType.测试服务器:
                    {
                        string str = Encoding.Unicode.GetString(data.Skip(1).ToArray());
                        Console.WriteLine("测试数据：{0}", str);
                    }
                    break;
            }
        }
        /// <summary>
        /// 监听Client服务端口
        /// </summary>
        public void ListenClientServerPort()
        {
            try
            {
                NetworkStream readStream = ClientServerTcp.GetStream();
                byte[] buffer = new byte[1024];
                while (true)
                {
                    int length = readStream.Read(buffer, 0, buffer.Length);
                    DoRecieveHClientServerPort(buffer, length, ClientServerTcp, false);
                }
            }
            catch (Exception ex)
            {
                Logger.Write("监听Client服务端口异常：\r\n{0}", ex);
                BreakClientServerTcp();
                BreakRemoteTcp();
            }
        }

        /// <summary>
        /// 监听Home服务端口
        /// </summary>
        public void ListenHomeServerPort()
        {
            try
            {
                NetworkStream readStream = HomeServerTcp.GetStream();
                byte[] buffer = new byte[1024];
                while (true)
                {
                    int length = readStream.Read(buffer, 0, buffer.Length);
                    DoRecieveHClientServerPort(buffer, length, HomeServerTcp, true);
                }
            }
            catch (Exception ex)
            {
                Logger.Write("监听Home服务端口异常：\r\n{0}", ex);
            }
        }

        /// <summary>
        /// 处理Home或Client服务端口数据
        /// </summary>
        /// <param name="asyncResult"></param>
        public void DoRecieveHClientServerPort(byte[] data, int length, TcpClient tcpClient, bool isFromHome)
        {
            if (length > 0)
            {
                try
                {
                    ServerTcp.WriteAsync(data, length, isFromHome ? MsgType.转发FromHome : MsgType.转发FromClient);
                }
                catch (Exception ex)
                {
                    Logger.Write("向服务器发送数据错误：\r\n{0}", ex);
                    ServerTcp = null;
                    try
                    {
                        tcpClient.Close();
                    }
                    catch { }
                    Logger.Write("断开{0}服务Tcp", isFromHome ? "Home" : "Client");
                }
            }
        }
        /// <summary>
        /// 向服务器发送心跳包
        /// </summary>
        public void SendHeartPackage()
        {
            try
            {
                ServerTcp.WriteAsync(new byte[] { 0 }, MsgType.心跳包);
            }
            catch (Exception ex)
            {
                ServerTcp = null;
            }
        }

        /// <summary>
        /// 断开远程Home/Client服务Tcp
        /// </summary>
        public void BreakRemoteTcp()
        {
            try
            {
                ServerTcp.WriteAsync(new byte[] { 0 }, MsgType.连接断开);
            }
            catch (Exception ex)
            {
                ServerTcp = null;
            }
        }
        /// <summary>
        /// 断开本地Home服务Tcp
        /// </summary>
        public void BreakHomeServerTcp()
        {
            if (_homeServerTcp == null) return;
            try
            {
                HomeServerTcp.Close();
            }
            catch { }
            HomeServerTcp = null;
        }
        /// <summary>
        /// 断开本地Client服务Tcp
        /// </summary>
        public void BreakClientServerTcp()
        {
            if (ClientServerTcp == null) return;
            try
            {
                ClientServerTcp.Close();
            }
            catch { }
            ClientServerTcp = null;
        }
        /// <summary>
        /// 测试服务
        /// </summary>
        public void TestServer()
        {
            ServerTcp.WriteAsync("测试", MsgType.测试服务器);
        }
    }
}
