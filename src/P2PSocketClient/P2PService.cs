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
                                DoTcpException(TcpErrorType.LocalServer, string.Format("[LocalServer]->[LocalPort] 端口:{0}连接失败!", ConfigServer.AppSettings.LocalServerPort));
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
            Logger.Info.WriteLine("[服务器] 连接中...{0}:{1}", ConfigServer.AppSettings.ServerIp, ConfigServer.AppSettings.ServerPort);
            try
            {
                //连接服务器
                ServerTcp = new TcpClient(ConfigServer.AppSettings.ServerIp, ConfigServer.AppSettings.ServerPort);
                Logger.Info.WriteLine("[服务器] 连接成功！");
                _taskFactory.StartNew(() =>
                {
                    RecieveServerTcp();
                });
                ret = true;
            }
            catch (Exception ex)
            {
                DoTcpException(TcpErrorType.Server, string.Format("[服务器] {0}:{1}连接失败！", ConfigServer.AppSettings.ServerIp, ConfigServer.AppSettings.ServerPort));
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
                    //发送Home服务名称
                    ServerTcp.WriteAsync(homeName, P2PSocketType.Local.Code, P2PSocketType.Local.ServerName.Code);
                    Logger.Info.WriteLine("[LocalServer]->[服务器] 成功启动LocalServer服务，Local服务名:{0} 端口:{1}", homeName,ConfigServer.AppSettings.LocalServerPort);
                }
                catch (Exception ex)
                {
                    DoTcpException(TcpErrorType.Server, "[LocalServer]->[服务器] 未连接到服务器，启动LocalServer服务失败！");
                }
            }
            else if (!IsEnableLocalServer)
            {
                Logger.Info.WriteLine("[LocalServer] 未配置LocalServerName，跳过启动LocalServer服务！");
            }
            else
            {
                Logger.Info.WriteLine("[LocalServer] 服务器未成功连接，跳过启动LocalServer服务！");
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
                    ServerTcp.WriteAsync(remoteServerName, P2PSocketType.Remote.Code, P2PSocketType.Remote.ServerName.Code);
                    Logger.Info.WriteLine("[RemoteServer]->[服务器] 成功启动Remote服务，Remote服务名:{0} 端口:{1}", remoteServerName,ConfigServer.AppSettings.RemoteLocalPort);
                    return true;
                }
                else
                {
                    Logger.Info.WriteLine("[RemoteServer]->[服务器] 未连接到服务器，启动Remote服务失败！");
                }
            }
            else
            {
                Logger.Info.WriteLine("[RemoteServer] 远程服务未启用！");
            }
            return ret;

        }
        public void StartRemoteServerListener()
        {
            try
            {
                if (ConfigServer.AppSettings.RemoteLocalPort > 0)
                {
                    RemoteServerListener = new TcpListener(IPAddress.Any, ConfigServer.AppSettings.RemoteLocalPort);
                    RemoteServerListener.Start();
                    Logger.Info.WriteLine("[RemoteServer] 成功启动RemoteServer服务，本地端口:{0}", ConfigServer.AppSettings.RemoteLocalPort);
                    _taskFactory.StartNew(() =>
                    {
                        try
                        {
                            while (true)
                            {
                                TcpClient tcpClient = RemoteServerListener.AcceptTcpClient();
                                if (!IsEnableRemoteServer)
                                {
                                    Logger.Info.WriteLine("[RemoteServer] Remote服务未启动，请设置远端Remote服务名，断开主动连入的tcp", tcpClient.Client.RemoteEndPoint);
                                    tcpClient.Close();
                                }
                                else if (RemoteServerTcp == null)
                                {
                                    RemoteServerTcp = tcpClient;
                                    _taskFactory.StartNew(() =>
                                    {
                                        ListenRemoteServerPort();
                                    });
                                }
                                else
                                {
                                    Logger.Info.WriteLine("[RemoteServer] 断开主动连入的tcp：{0}", tcpClient.Client.RemoteEndPoint);
                                    tcpClient.Close();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            DoTcpException(TcpErrorType.RemoteServer, string.Format("[RemoteServer] 端口监听错误:\r\n{0}", ex));
                        }
                    });
                }
                else
                {
                    Logger.Info.WriteLine("[RemoteServer] 未配置RemoteServerPort，跳过启动RemoteServer服务！");
                }
            }
            catch (Exception ex)
            {
                DoTcpException(TcpErrorType.RemoteServer, string.Format("[RemoteServer] 启动RemoteServer服务失败，端口:{0}监听失败！", ConfigServer.AppSettings.RemoteLocalPort));
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
                    Logger.Trace.WriteLine("[服务器] 接收到数据，长度:{0}", length);
                    ConcurrentQueue<byte[]> results = tcpHelper.ReadPackages(buffer, length);
                    while (!results.IsEmpty)
                    {
                        byte[] data;
                        if (results.TryDequeue(out data))
                        {
                            HandleServerOnePackageData(data, ServerTcp);
                        }
                    }
                }
                else
                {
                    Logger.Trace.WriteLine("[服务器] 接收到数据，长度:{0}，断开连接！", length);
                    break;
                }
            }
            DoTcpException(TcpErrorType.Server, "[服务器] tcp连接断开！");
        }

        /// <summary>
        /// 处理来自服务器的数据
        /// </summary>
        /// <param name="data">完整的数据包</param>
        /// <param name="tcpResult"></param>
        //public void ReievedServiceTcpCallBack(byte[] data, TcpResult tcpResult)
        public void HandleServerOnePackageData(byte[] data, TcpClient tcpClient)
        {
            switch (data[0])
            {
                case P2PSocketType.Heart.Code:
                    {
                    }
                    break;
                case P2PSocketType.Http.Code:
                    {
                        HandleHttpPackage(data[1], data.Skip(2).ToArray());
                    }
                    break;
                case P2PSocketType.Local.Code:
                    {
                        HandleLocalPackage(data[1], data.Skip(2).ToArray());
                    }
                    break;
                case P2PSocketType.Remote.Code:
                    {
                        HandleRemotePackage(data[1], data.Skip(2).ToArray());
                    }
                    break;
                case P2PSocketType.Secure.Code:
                    {
                    }
                    break;
            }
        }
        public void HandleLocalPackage(byte type, byte[] data)
        {
            switch (type)
            {
                case P2PSocketType.Local.Break.Code:
                    {
                        BreakLocalServerTcp();
                    }
                    break;
                case P2PSocketType.Local.Error.Code:
                    {
                        BreakLocalServerTcp();
                    }
                    break;
                case P2PSocketType.Local.Secure.Code:
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
                                        DoTcpException(TcpErrorType.LocalServer, string.Format("[LocalServer]->[LocalPort] 端口:{0}连接失败!", ConfigServer.AppSettings.LocalServerPort));
                                    }
                                }
                            }
                        }
                    }
                    break;
                case P2PSocketType.Local.ServerName.Code:
                    {
                    }
                    break;
                case P2PSocketType.Local.Transfer.Code:
                    {
                        try
                        {
                            LocalServerTcp.WriteAsync(data);
                        }
                        catch (Exception ex)
                        {
                            DoTcpException(TcpErrorType.LocalServer, string.Format("[LocalServer]->[Port] 数据转发错误，长度:{0}\r\n{1}", data.Length, ex));
                        }
                    }
                    break;
            }
        }
        public void HandleRemotePackage(byte type, byte[] data)
        {
            switch (type)
            {
                case P2PSocketType.Remote.Break.Code:
                    {
                        BreakRemoteServerTcp();
                    }
                    break;
                case P2PSocketType.Remote.Error.Code:
                    {
                        BreakRemoteServerTcp();
                    }
                    break;
                case P2PSocketType.Remote.Secure.Code:
                    {
                    }
                    break;
                case P2PSocketType.Remote.ServerName.Code:
                    {
                    }
                    break;
                case P2PSocketType.Remote.Transfer.Code:
                    {
                        try
                        {
                            RemoteServerTcp.WriteAsync(data);
                        }
                        catch (Exception ex)
                        {
                            DoTcpException(TcpErrorType.RemoteServer, string.Format("[RemoteServer]->[Port] 数据转发错误，长度:{0}\r\n{1}", data.Length, ex));
                        }
                    }
                    break;
            }
        }
        public void HandleHttpPackage(byte type, byte[] data)
        {
            if (m_httpServer != null)
                m_httpServer.HandleHttpPackage(type, data);

        }
        /// <summary>
        /// 监听Remote服务端口
        /// </summary>
        public void ListenRemoteServerPort()
        {
            EndPoint endPoint = RemoteServerTcp.Client.RemoteEndPoint;
            Logger.Info.WriteLine("[RemoteServer] 新联入Tcp:{0}", endPoint);
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
                catch (Exception ex)
                {
                    Logger.Info.WriteLine("[Port]->[RemoteServer] 接收数据错误:\r\n{0}", ex);
                }
                if (length > 0)
                {
                    Logger.Trace.WriteLine("[Port]->[RemoteServer] 接收到数据，长度:{0}", length);
                    TransferRemoteServerDataToServer(buffer, length, RemoteServerTcp);
                }
                else
                {
                    Logger.Trace.WriteLine("[Port]->[RemoteServer] 接收到数据，长度:{0}，断开连接！", length);
                    break;
                }
            }
            DoTcpException(TcpErrorType.RemoteServer, string.Format("[RemoteServer] 已断开Tcp:{0}", endPoint));
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
                catch (Exception ex)
                {
                    Logger.Info.WriteLine("[Port]->[LocalServer] 接收数据错误:\r\n{0}", ex);
                }
                if (length > 0)
                {
                    Logger.Trace.WriteLine("[Port]->[LocalServer] 接收到数据，长度:{0}", length);
                    TransferLocalServerDataToServer(buffer, length, LocalServerTcp);
                }
                else
                {
                    Logger.Trace.WriteLine("[Port]->[LocalServer] 接收到数据，长度:{0}，断开连接！", length);
                    break;
                }
            }
            DoTcpException(TcpErrorType.LocalServer, string.Format("[LocalServer] 已断开Tcp:{0}", endPoint));
        }
        public void TransferRemoteServerDataToServer(byte[] data, int length, TcpClient tcpClient)
        {
            try
            {
                Logger.Trace.WriteLine("[RemoteServer]->[服务器] 发送数据，长度:{0}", length);
                ServerTcp.WriteAsync(data, length, P2PSocketType.Remote.Code, P2PSocketType.Remote.Transfer.Code);
            }
            catch (Exception ex)
            {
                DoTcpException(TcpErrorType.Server, "[服务器] 连接失败！");
            }
        }
        public void TransferLocalServerDataToServer(byte[] data, int length, TcpClient tcpClient)
        {
            try
            {
                Logger.Trace.WriteLine("[LocalServer]->[服务器] 发送数据，长度:{0}", length);
                ServerTcp.WriteAsync(data, length, P2PSocketType.Local.Code, P2PSocketType.Local.Transfer.Code);
            }
            catch (Exception ex)
            {
                DoTcpException(TcpErrorType.Server, "[服务器] 连接失败！");
            }
        }

        /// <summary>
        /// 向服务器发送心跳包
        /// </summary>
        public void SendHeartPackage()
        {
            try
            {
                ServerTcp.WriteAsync(new byte[] { 0 }, P2PSocketType.Heart.Code);
            }
            catch
            {
                ServerTcp = null;
            }
        }

        /// <summary>
        /// 断开远程Home/Client服务Tcp
        /// </summary>
        public void BreakRemoteTcp(byte type1, byte type2)
        {
            try
            {
                ServerTcp.WriteAsync(new byte[] { 0 }, type1, type2);
            }
            catch
            {
                ServerTcp = null;
            }
        }
        /// <summary>
        /// 断开本地Home服务Tcp
        /// </summary>
        public void BreakLocalServerTcp()
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
        public void BreakRemoteServerTcp()
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
                Logger.Error.WriteLine(errorMsg);
            }
            switch (type)
            {
                case TcpErrorType.Server:
                    {
                        BreakLocalServerTcp();
                        BreakRemoteServerTcp();
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
                        BreakLocalServerTcp();
                        BreakRemoteTcp(P2PSocketType.Local.Code, P2PSocketType.Local.Break.Code);
                    }
                    break;
                case TcpErrorType.RemoteServer:
                    {
                        BreakRemoteServerTcp();
                        BreakRemoteTcp(P2PSocketType.Remote.Code, P2PSocketType.Remote.Break.Code);
                    }
                    break;
            }
        }

        internal enum TcpErrorType
        {
            Server,
            LocalServer,
            RemoteServer
        }
    }
}
