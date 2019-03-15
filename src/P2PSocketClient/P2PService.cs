using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using Wireboy.Socket.P2PClient;
using Wireboy.Socket.P2PClient.Models;

namespace P2PServiceHome
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
        bool HomeServerTcpIsNull { get { return _homeServerTcp == null; } }
        TcpClient HomeServerTcp
        {
            set { _homeServerTcp = value; }
            get
            {
                if (_homeServerTcp == null && IsEnableHome)
                    lock (_lockHomeServerTcp)
                    {
                        try
                        {
                            if (_homeServerTcp == null) _homeServerTcp = new TcpClient("127.0.0.1", ConfigServer.AppSettings.LocalHomePort);
                            _taskFactory.StartNew(() =>
                            {
                                try
                                {
                                    ListenHomeServerPort();
                                }
                                catch (Exception ex)
                                {
                                    Logger.Write("监听Home服务端口异常：\r\n{0}", ex);
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            Logger.Write("连接本地服务失败：\r\n{0}", ex);
                        }
                    }
                return _homeServerTcp;
            }
        }
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
            if (ServerTcp != null)
            {
                try
                {
                    ConfigServer.AppSettings.HomeServerName = homeName;
                    //发送Home服务名称
                    ServerTcp.WriteAsync(homeName, MsgType.本地服务名);
                }
                catch (Exception ex)
                {
                    try
                    {
                        ServerTcp.Close();
                    }
                    catch { }
                    ServerTcp = null;
                    Logger.Write("启动Home服务异常：\r\n{0}", ex);
                }
            }

        }
        public void StartClientServer(string homeName)
        {
            IsEnableClient = true;
            if (ServerTcp != null)
            {
                ServerTcp.WriteAsync(homeName, MsgType.远程服务名);
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
                    }
                });
            }
        }

        /// <summary>
        /// 接收服务器数据
        /// </summary>
        public void RecieveServerTcp()
        {
            try
            {
                NetworkStream readStream = ServerTcp.GetStream();
                TcpResult tcpResult = new TcpResult(readStream, ServerTcp, ReievedServiceTcpCallBack);
                while (true)
                {
                    int length = readStream.Read(tcpResult.Readbuffer, 0, tcpResult.Readbuffer.Length);
                    DoRecieveClientTcp(tcpResult, length);
                    tcpResult.ResetReadBuffer();
                }
            }
            catch (Exception ex)
            {
                Logger.Write("接收服务器数据异常：\r\n{0}", ex);
            }
        }

        /// <summary>
        /// 接收服务器数据回调方法
        /// </summary>
        /// <param name="asyncResult"></param>
        public void DoRecieveClientTcp(TcpResult tcpResult, int length)
        {
            if (length > 0)
            {
                string log = string.Format("从{0}接收到长度{1}的数据,类型:{2} - {3}", tcpResult.ReadTcp.Client.RemoteEndPoint, length, tcpResult.Readbuffer[2], Enum.GetName(typeof(MsgType), tcpResult.Readbuffer[2]));
                Logger.Debug(log);
                int curReadIndex = 0;
                do
                {
                    tcpResult.ReadOnePackageData(length, ref curReadIndex);
                } while (curReadIndex <= length - 1);
            }
        }
        /// <summary>
        /// 接收一个完整数据包回调方法
        /// </summary>
        /// <param name="data">完整的数据包</param>
        /// <param name="tcpResult"></param>
        public void ReievedServiceTcpCallBack(byte[] data, TcpResult tcpResult)
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
                                Logger.Debug("转发数据到HomeService");
                                HomeServerTcp.WriteAsync(data.Skip(1).ToArray(), MsgType.不封包);
                            }
                            catch (Exception ex)
                            {
                                HomeServerTcp = null;
                                SendSocketBreak(ServerTcp);
                                Logger.Write("向本地端口发送数据错误：\r\n{0}", ex);
                            }
                        }
                        else
                        {
                            Logger.Debug("转发数据到HomeService失败，没有连接到Home服务的TCP");
                        }
                    }
                    break;
                case (byte)MsgType.转发FromHome:
                    {
                        if (ClientServerTcp != null)
                        {
                            try
                            {
                                Logger.Debug("转发数据到HomeService");
                                ClientServerTcp.WriteAsync(data.Skip(1).ToArray(), MsgType.不封包);
                            }
                            catch (Exception ex)
                            {
                                ClientServerTcp = null;
                                SendSocketBreak(ServerTcp);
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
                        try
                        {
                            if (_homeServerTcp != null)
                                HomeServerTcp.Close();
                            HomeServerTcp = null;
                        }
                        catch (Exception ex)
                        {
                            HomeServerTcp = null;
                        }
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
                TcpResult tcpResult = new TcpResult(readStream, ClientServerTcp, null);
                while (true)
                {
                    int length = readStream.Read(tcpResult.Readbuffer, 0, tcpResult.Readbuffer.Length);
                    Logger.Debug("接收到长度{0}的数据，来自：Client服务", length);
                    DoRecieveClientServerPort(tcpResult, length);
                    tcpResult.ResetReadBuffer();
                }
            }catch(Exception ex)
            {
                Logger.Write("监听Client服务端口异常：\r\n{0}", ex);
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
                TcpResult tcpResult = new TcpResult(readStream, HomeServerTcp, null);
                while (true)
                {
                    int length = readStream.Read(tcpResult.Readbuffer, 0, tcpResult.Readbuffer.Length);
                    Logger.Debug("接收到长度{0}的数据，来自：Home服务", length);
                    DoRecieveHomeServerPort(tcpResult, length);
                    tcpResult.ResetReadBuffer();
                }
            }
            catch (Exception ex)
            {
                Logger.Write("监听Home服务端口异常：\r\n{0}", ex);
            }
        }

        /// <summary>
        /// 处理Client服务端口数据
        /// </summary>
        /// <param name="asyncResult"></param>
        public void DoRecieveClientServerPort(TcpResult tcpResult, int length)
        {
            if (length > 0)
            {
                try
                {
                    ServerTcp.WriteAsync(tcpResult.Readbuffer, length, MsgType.转发FromClient);
                }
                catch (Exception ex)
                {
                    Logger.Write("向服务器发送数据错误：\r\n{0}", ex);
                    ServerTcp = null;
                    try
                    {
                        ClientServerTcp.Close();
                    }
                    catch { }
                    Logger.Write("断开本地其它服务Tcp");
                    ClientServerTcp = null;
                }
            }
        }

        /// <summary>
        /// 处理Home服务端口数据
        /// </summary>
        /// <param name="asyncResult"></param>
        public void DoRecieveHomeServerPort(TcpResult tcpResult, int length)
        {
            if (length > 0)
            {
                try
                {
                    ServerTcp.WriteAsync(tcpResult.Readbuffer, length, MsgType.转发FromHome);
                }
                catch (Exception ex)
                {
                    Logger.Write("向服务器发送数据错误：\r\n{0}", ex);
                    ServerTcp = null;
                    try
                    {
                        HomeServerTcp.Close();
                    }
                    catch (Exception ex1)
                    {

                    }
                    Logger.Write("断开Home服务Tcp");
                    HomeServerTcp = null;
                }
            }
        }

        /// <summary>
        /// 向指定tcp发送连接断开信息
        /// </summary>
        /// <param name="serverClient"></param>
        public void SendSocketBreak(TcpClient serverClient)
        {
            try
            {
                serverClient.WriteAsync(new byte[] { 0 }, MsgType.连接断开);
            }
            catch (Exception ex)
            {
                serverClient = null;
            }
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
