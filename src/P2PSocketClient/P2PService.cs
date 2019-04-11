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
using Wireboy.Socket.P2PClient.Services;

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
        TcpClient m_localServerTcp = null;
        object m_lockLocalServerTcp = new object();

        /// <summary>
        /// 本地服务Tcp连接（注：不存在则创建，不应使用此属性判断null）
        /// </summary>
        TcpClient LocalServerTcp
        {
            set { m_localServerTcp = null; }
            get
            {
                if (IsEnableLocalServer && (m_localServerTcp == null || !m_localServerTcp.Connected))
                {
                    lock (m_lockLocalServerTcp)
                    {
                        if (m_localServerTcp == null || !m_localServerTcp.Connected)
                        {
                            try
                            {
                                m_localServerTcp = new TcpClient("127.0.0.1", ConfigServer.AppSettings.LocalServerPort);
                                _taskFactory.StartNew(() =>
                                    {
                                        ListenLocalServerPort();
                                    });
                            }
                            catch (Exception ex)
                            {
                                DoTcpException(TcpErrorType.LocalServer, string.Format("本地服务-tcp连接失败-端口:{0}", ConfigServer.AppSettings.LocalServerPort));
                            }
                        }
                    }
                }
                return m_localServerTcp;
            }
        }
        /// <summary>
        /// 远程服务Tcp
        /// </summary>
        TcpClient RemoteServerTcp { get; set; } = null;
        /// <summary>
        /// 远程服务Listener
        /// </summary>
        TcpListener RemoteServerListener { set; get; }
        /// <summary>
        /// 服务器Tcp连接
        /// </summary>
        public TcpClient ServerTcp { set; get; } = null;
        /// <summary>
        /// 本地服务是否启用
        /// </summary>
        public bool IsEnableLocalServer
        {
            get
            {
                return !string.IsNullOrEmpty(ConfigServer.AppSettings.LocalServerName);
            }
        }
        /// <summary>
        /// 是否已设置远程服务名称
        /// </summary>
        public bool IsEnableRemoteServer
        {
            get { return !string.IsNullOrEmpty(RemoteServerName); }
        }
        /// <summary>
        /// 远程服务名称
        /// </summary>
        private string RemoteServerName { set; get; }

        HttpServer m_httpServer;


        public P2PService()
        {
        }
        /// <summary>
        /// 启动p2p服务
        /// </summary>
        public bool Start()
        {
            bool ret = false;
            //连接服务器
            if (ConnectServer())
            {
                //启动LocalServer
                SetLocalServerName(ConfigServer.AppSettings.LocalServerName);
                //启动RemoteServer
                StartRemoteServerListener();
                //启动Http服务
                StartHttpServer();
                //启动守护线程
                _taskFactory.StartNew(() => { CheckAndReconnectServer(); });
                ret = true;
            }
            return ret;
        }

        /// <summary>
        /// 检查并重连服务器Tcp
        /// </summary>
        public void CheckAndReconnectServer()
        {
            while (true)
            {
                if (ServerTcp == null)
                {
                    if (ConnectServer())
                    {
                        //启动LocalServer
                        SetLocalServerName(ConfigServer.AppSettings.LocalServerName);
                        //启动RemoteServer
                        if (!string.IsNullOrEmpty(RemoteServerName))
                            SetRemoteServerName(RemoteServerName);
                        //启动Http服务
                        StartHttpServer();
                    }
                }
                else
                {
                    SendHeartPackage();
                }
                Thread.Sleep(2000);
            }
        }

        /// <summary>
        /// 连接服务器
        /// </summary>
        public bool ConnectServer()
        {
            bool ret = false;
            Logger.WriteLine("服务器-连接中...{0}:{1}",  ConfigServer.AppSettings.ServerIp, ConfigServer.AppSettings.ServerPort);
            try
            {
                //连接服务器
                ServerTcp = new TcpClient(ConfigServer.AppSettings.ServerIp, ConfigServer.AppSettings.ServerPort);
                Logger.WriteLine("服务器-连接成功！");
                _taskFactory.StartNew(() =>
                {
                    RecieveServerTcp();
                });
                ret = true;
            }
            catch (Exception ex)
            {
                DoTcpException(TcpErrorType.Server, string.Format("服务器-{0}:{1}连接失败！", ConfigServer.AppSettings.ServerIp, ConfigServer.AppSettings.ServerPort));
            }
            return ret;
        }

        public void StartHttpServer()
        {
            m_httpServer = new HttpServer(this);
            m_httpServer.Start();
        }

        /// <summary>
        /// 启动Home服务
        /// </summary>
        /// <param name="homeName"></param>
        public void SetLocalServerName(string homeName)
        {
            if (IsEnableLocalServer && ServerTcp != null)
            {
                try
                {
                    Logger.WriteLine("本地服务-设置服务名:{0}",  homeName);
                    //发送Home服务名称
                    ServerTcp.WriteAsync(homeName, MsgType.本地服务名);
                }
                catch (Exception ex)
                {
                    DoTcpException(TcpErrorType.Server, "服务器-连接失败！");
                }
            }

        }
        public bool SetRemoteServerName(string remoteServerName)
        {
            bool ret = false;
            if (RemoteServerListener != null)
            {
                RemoteServerName = remoteServerName;
                if (ServerTcp != null)
                {
                    Logger.WriteLine("远程服务-连接服务名：{0}", remoteServerName);
                    ServerTcp.WriteAsync(remoteServerName, MsgType.远程服务名);
                    return true;
                }
            }
            return ret;

        }
        public void StartRemoteServerListener()
        {
            Logger.WriteLine("远程服务-监听本地端口:{0}", ConfigServer.AppSettings.RemoteLocalPort);
            try
            {
                RemoteServerListener = new TcpListener(IPAddress.Any, ConfigServer.AppSettings.RemoteLocalPort);
                RemoteServerListener.Start();
                _taskFactory.StartNew(() =>
                {
                    try
                    {
                        while (true)
                        {
                            TcpClient tcpClient = RemoteServerListener.AcceptTcpClient();
                            if (IsEnableRemoteServer && RemoteServerTcp == null)
                            {
                                RemoteServerTcp = tcpClient;
                                _taskFactory.StartNew(() =>
                                {
                                    ListenRemoteServerPort();
                                });
                            }
                            else
                            {
                                Logger.WriteLine("远程服务-断开主动连入的tcp：{0}", tcpClient.Client.RemoteEndPoint);
                                tcpClient.Close();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        DoTcpException(TcpErrorType.Client, "远程服务-本地端口监听失败！");
                    }
                });
            }
            catch (Exception ex)
            {
                DoTcpException(TcpErrorType.Client, string.Format("远程服务-本地端口:{0}监听失败！", ConfigServer.AppSettings.RemoteLocalPort));
            }
        }

        /// <summary>
        /// 接收服务器数据
        /// </summary>
        public void RecieveServerTcp()
        {
            NetworkStream readStream = ServerTcp.GetStream();
            TcpHelper tcpHelper = new TcpHelper();
            byte[] buffer = new byte[1024];
            int length = 0;
            while (readStream.CanRead)
            {
                length = 0;
                try
                {
                    length = readStream.Read(buffer, 0, buffer.Length);
                }
                catch { }
                if (length > 0)
                {
                    ConcurrentQueue<byte[]> results = tcpHelper.ReadPackages(buffer, length);
                    while (!results.IsEmpty)
                    {
                        byte[] data;
                        if (results.TryDequeue(out data))
                        {
                            ReievedServiceTcpCallBack(data, ServerTcp);
                        }
                    }
                }
                else
                {
                    break;
                }
            }
            DoTcpException(TcpErrorType.Server, "服务器-tcp连接断开！");
        }

        /// <summary>
        /// 处理来自服务器的数据
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
                case (byte)MsgType.转发FromRemote:
                    {
                        if (IsEnableLocalServer)
                        {
                            try
                            {
                                LocalServerTcp.WriteAsync(data.Skip(1).ToArray(), MsgType.不封包);
                            }
                            catch (Exception ex)
                            {
                                DoTcpException(TcpErrorType.LocalServer, "本地服务-数据转发失败！");
                            }
                        }
                    }
                    break;
                case (byte)MsgType.转发FromLocal:
                    {
                        if (RemoteServerTcp != null)
                        {
                            try
                            {
                                RemoteServerTcp.WriteAsync(data.Skip(1).ToArray(), MsgType.不封包);
                            }
                            catch (Exception ex)
                            {
                                DoTcpException(TcpErrorType.Client, "远程服务-转发数据失败！");
                            }
                        }
                    }
                    break;
                case (byte)MsgType.断开FromRemote:
                    {
                        BreakHomeServerTcp();
                    }
                    break;
                case (byte)MsgType.断开FromLocal:
                    {
                        BreakClientServerTcp();
                    }
                    break;
                case (byte)MsgType.测试服务器:
                    {
                        string str = Encoding.Unicode.GetString(data.Skip(1).ToArray());
                        Logger.WriteLine("测试数据：{0}", str);
                    }
                    break;
                case (byte)MsgType.Http服务:
                    {
                        if (m_httpServer != null)
                            m_httpServer.RecieveServerTcp(data.Skip(1).ToArray());
                    }
                    break;
            }
        }
        /// <summary>
        /// 监听Remote服务端口
        /// </summary>
        public void ListenRemoteServerPort()
        {
            EndPoint endPoint = RemoteServerTcp.Client.RemoteEndPoint;
            Logger.WriteLine("远程服务-新联入Tcp:{0}", endPoint);
            NetworkStream readStream = RemoteServerTcp.GetStream();
            byte[] buffer = new byte[1024];
            int length = 0;
            while (readStream.CanRead)
            {
                length = 0;
                try
                {
                    length = readStream.Read(buffer, 0, buffer.Length);
                }
                catch { }
                if (length > 0)
                {
                    DoRecieveLocalRemoteServerPort(buffer, length, RemoteServerTcp, false);
                }
                else
                {
                    break;
                }
            }
            DoTcpException(TcpErrorType.Client, string.Format("远程服务-已断开Tcp:{0}", endPoint));
        }

        /// <summary>
        /// 监听Local服务端口
        /// </summary>
        public void ListenLocalServerPort()
        {
            EndPoint endPoint = LocalServerTcp.Client.RemoteEndPoint;
            NetworkStream readStream = LocalServerTcp.GetStream();
            byte[] buffer = new byte[1024];
            int length = 0;
            while (readStream.CanRead)
            {
                length = 0;
                try
                {
                    length = readStream.Read(buffer, 0, buffer.Length);
                }
                catch { }
                if (length > 0)
                {
                    DoRecieveLocalRemoteServerPort(buffer, length, LocalServerTcp, true);
                }
                else
                {
                    break;
                }
            }
            DoTcpException(TcpErrorType.LocalServer, string.Format("本地服务-已断开Tcp:{0}", endPoint));
        }

        /// <summary>
        /// 处理Local或Remote服务端口数据
        /// </summary>
        /// <param name="asyncResult"></param>
        public void DoRecieveLocalRemoteServerPort(byte[] data, int length, TcpClient tcpClient, bool isFromHome)
        {
            //Logger.Write("接收到Client服务数据,长度：{0}", length);
            try
            {
                ServerTcp.WriteAsync(data, length, isFromHome ? MsgType.转发FromLocal : MsgType.转发FromRemote);
            }
            catch (Exception ex)
            {
                DoTcpException(TcpErrorType.Server, "服务器-连接失败！");
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
            catch
            {
                ServerTcp = null;
            }
        }

        /// <summary>
        /// 断开远程Home/Client服务Tcp
        /// </summary>
        public void BreakRemoteTcp(MsgType msgType)
        {
            try
            {
                ServerTcp.WriteAsync(new byte[] { 0 }, msgType);
            }
            catch
            {
                ServerTcp = null;
            }
        }
        /// <summary>
        /// 断开本地Home服务Tcp
        /// </summary>
        public void BreakHomeServerTcp()
        {
            if (m_localServerTcp == null) return;
            try
            {
                m_localServerTcp.Close();
            }
            catch { }
            m_localServerTcp = null;
        }
        /// <summary>
        /// 断开本地Client服务Tcp
        /// </summary>
        public void BreakClientServerTcp()
        {
            if (RemoteServerTcp == null) return;
            try
            {
                RemoteServerTcp.Close();
            }
            catch { }
            RemoteServerTcp = null;
        }

        internal void DoTcpException(TcpErrorType type, string errorMsg)
        {
            if (!string.IsNullOrEmpty(errorMsg))
            {
                Logger.WriteLine(errorMsg);
            }
            switch (type)
            {
                case TcpErrorType.Server:
                    {
                        BreakHomeServerTcp();
                        BreakClientServerTcp();
                        try
                        {
                            //尝试关闭TCP连接
                            ServerTcp.Close();
                        }
                        catch { }
                        ServerTcp = null;
                    }
                    break;
                case TcpErrorType.LocalServer:
                    {
                        BreakHomeServerTcp();
                        BreakRemoteTcp(MsgType.断开FromLocal);
                    }
                    break;
                case TcpErrorType.Client:
                    {
                        BreakClientServerTcp();
                        BreakRemoteTcp(MsgType.断开FromRemote);
                    }
                    break;
            }
        }

        internal enum TcpErrorType
        {
            Server,
            LocalServer,
            Client
        }
    }
}
