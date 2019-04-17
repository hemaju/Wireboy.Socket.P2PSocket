using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using Wireboy.Socket.P2PService.Models;
using System.Collections.Concurrent;
using Wireboy.Socket.P2PService.Services;

namespace Wireboy.Socket.P2PService
{
    public class P2PService
    {
        public TcpClientMapHelper _tcpMapHelper = new TcpClientMapHelper();
        private TaskFactory _taskFactory = new TaskFactory();
        private HttpServer _httpServer;
        public P2PService()
        {
            _httpServer = new HttpServer(this);
        }

        public void Start()
        {
            _httpServer.Start();
            //监听通讯端口
            ListenServerPort();
        }

        /// <summary>
        /// 监听通讯端口
        /// </summary>
        public void ListenServerPort()
        {
            Logger.Info.WriteLine("[服务器] 监听本地端口：{0}", ConfigServer.AppSettings.ServerPort);
            TcpListener listener = null;
            try
            {
                listener = new TcpListener(IPAddress.Any, ConfigServer.AppSettings.ServerPort);
                listener.Start();
            }
            catch (Exception ex)
            {
                Logger.Error.WriteLine("[服务器] 监听端口错误：\r\n{0}", ex);
                return;
            }
            while (true)
            {
                TcpClient tcpClient = listener.AcceptTcpClient();
                Logger.Debug.WriteLine("[Port]->[服务器] 接收到来自{0}的tcp接入", tcpClient.Client.RemoteEndPoint);
                _taskFactory.StartNew(() =>
                {
                    RecieveClientTcp(tcpClient);
                });
            }
        }

        public void RecieveClientTcp(TcpClient readTcp)
        {
            EndPoint endPoint = readTcp.Client.RemoteEndPoint;
            NetworkStream readStream = readTcp.GetStream();
            TcpHelper tcpHelper = new TcpHelper();
            byte[] buffer = new byte[10240];
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
                    Logger.Error.WriteLine(string.Format("[Port]->[服务器] 来自{0}的tcp流读取错误：\r\n{1}", endPoint, ex));
                }
                if (length > 0)
                {
                    Logger.Trace.WriteLine(string.Format("[Port]->[服务器] 接收到数据，长度:{1} 地址:{0}", endPoint, length));
                    ConcurrentQueue<byte[]> results = tcpHelper.ReadPackages(buffer, length);
                    while (!results.IsEmpty)
                    {
                        byte[] data;
                        if (results.TryDequeue(out data))
                        {
                            HandleOnePackage(data, readTcp);
                        }
                    }
                }
                else
                {
                    Logger.Trace.WriteLine(string.Format("[Port]->[服务器] 接收到数据，断开连接！长度:{1} 地址:{0}", endPoint, length));
                    break;
                }
            }
            Logger.Debug.WriteLine("[服务器] 断开tcp连接：{0}\r\n", endPoint);
            CloseRelateTcp(readTcp);
        }

        public void HandleOnePackage(byte[] data, TcpClient tcpClient)
        {
            switch (data[0])
            {
                case P2PSocketType.Heart.Code:
                    {
                    }
                    break;
                case P2PSocketType.Http.Code:
                    {
                        _httpServer.HandleHttpPackage(data[1], data.Skip(2).ToArray(), tcpClient);
                    }
                    break;
                case P2PSocketType.Local.Code:
                    {
                        HandleLocalPackage(data[1], data.Skip(2).ToArray(), tcpClient);
                    }
                    break;
                case P2PSocketType.Remote.Code:
                    {
                        HandleRemotePackage(data[1], data.Skip(2).ToArray(), tcpClient);
                    }
                    break;
                case P2PSocketType.Secure.Code:
                    {
                    }
                    break;
            }
        }
        public void HandleLocalPackage(byte type, byte[] data, TcpClient tcpClient)
        {
            TcpClient toClient = _tcpMapHelper[tcpClient, false];
            switch (type)
            {
                case P2PSocketType.Local.Break.Code:
                    {
                        if (toClient != null)
                        {
                            try
                            {
                                toClient.WriteAsync(data.ToArray(), P2PSocketType.Remote.Code, P2PSocketType.Remote.Transfer.Code);
                                Logger.Trace.WriteLine("[服务器]->[RemoteServer] 发送Break数据，长度:{0}", data.Length);
                            }
                            catch (Exception ex)
                            {
                                Logger.Trace.WriteLine("[服务器]->[RemoteServer] 发送Break数据失败！{0} \r\n{1}", data.Length, ex);
                                _tcpMapHelper[tcpClient, false] = null;
                            }
                        }
                    }
                    break;
                case P2PSocketType.Local.Error.Code:
                    {

                        try
                        {
                            toClient.WriteAsync(data.ToArray(), P2PSocketType.Remote.Code, P2PSocketType.Remote.Error.Code);
                            Logger.Trace.WriteLine("[服务器]->[RemoteServer] 发送Error数据，长度:{0}", data.Length);
                        }
                        catch (Exception ex)
                        {
                            Logger.Trace.WriteLine("[服务器]->[RemoteServer] 发送Error数据失败！长度:{0} \r\n{1}", data.Length, ex);
                            _tcpMapHelper[tcpClient, false] = null;
                        }
                    }
                    break;
                case P2PSocketType.Local.Secure.Code:
                    {
                        try
                        {
                            toClient.WriteAsync(data.ToArray(), P2PSocketType.Remote.Code, P2PSocketType.Remote.Secure.Code);
                            Logger.Trace.WriteLine("[服务器]->[RemoteServer] 发送Secure数据，长度:{0}", data.Length);
                        }
                        catch (Exception ex)
                        {
                            Logger.Trace.WriteLine("[服务器]->[RemoteServer] 发送Secure数据失败！长度:{0} \r\n{1}", data.Length, ex);
                            _tcpMapHelper[tcpClient, false] = null;
                        }
                    }
                    break;
                case P2PSocketType.Local.ServerName.Code:
                    {
                        string key = data.ToStringUnicode();
                        _tcpMapHelper.SetLocalServerClinet(tcpClient, key);
                        Logger.Debug.WriteLine("[LocalServerClient]->[服务器] 设置Local服务名 ip:{0} key:{1}", tcpClient.Client.RemoteEndPoint, key);
                    }
                    break;
                case P2PSocketType.Local.Transfer.Code:
                    {
                        if (toClient != null)
                        {
                            try
                            {
                                toClient.WriteAsync(data.ToArray(), P2PSocketType.Remote.Code, P2PSocketType.Remote.Transfer.Code);
                                Logger.Trace.WriteLine("[服务器]->[RemoteServer] 发送Transfer数据，长度:{0}", data.Length);
                            }
                            catch (Exception ex)
                            {
                                Logger.Trace.WriteLine("[服务器]->[RemoteServer] 发送Transfer数据失败！长度:{0} \r\n{1}", data.Length, ex);
                                _tcpMapHelper[tcpClient, false] = null;
                            }
                        }
                    }
                    break;
            }
        }
        public void HandleRemotePackage(byte type, byte[] data, TcpClient tcpClient)
        {
            TcpClient toClient = _tcpMapHelper[tcpClient, true];
            switch (type)
            {
                case P2PSocketType.Remote.Break.Code:
                    {
                        if (toClient != null)
                        {
                            try
                            {
                                toClient.WriteAsync(data.ToArray(), P2PSocketType.Local.Code, P2PSocketType.Local.Break.Code);
                                Logger.Trace.WriteLine("[服务器]->[LocalServer] 发送Break数据，长度:{0}", data.Length);
                            }
                            catch (Exception ex)
                            {
                                Logger.Trace.WriteLine("[服务器]->[LocalServer] 发送Break数据失败！长度:{0} \r\n{1}", data.Length, ex);
                                _tcpMapHelper[tcpClient, true] = null;
                            }
                        }
                    }
                    break;
                case P2PSocketType.Remote.Error.Code:
                    {
                        if (toClient != null)
                        {
                            try
                            {
                                toClient.WriteAsync(data.ToArray(), P2PSocketType.Local.Code, P2PSocketType.Local.Error.Code);
                                Logger.Trace.WriteLine("[服务器]->[LocalServer] 发送Error数据，长度:{0}", data.Length);
                            }
                            catch (Exception ex)
                            {
                                Logger.Trace.WriteLine("[服务器]->[LocalServer] 发送Error数据失败！长度:{0} \r\n{1}", data.Length, ex);
                                _tcpMapHelper[tcpClient, true] = null;
                            }
                        }
                    }
                    break;
                case P2PSocketType.Remote.Secure.Code:
                    {
                        if (toClient != null)
                        {
                            try
                            {
                                toClient.WriteAsync(data.ToArray(), P2PSocketType.Local.Code, P2PSocketType.Local.Secure.Code);
                                Logger.Trace.WriteLine("[服务器]->[LocalServer] 发送Secure数据，长度:{0}", data.Length);
                            }
                            catch (Exception ex)
                            {
                                Logger.Trace.WriteLine("[服务器]->[LocalServer] 发送Secure数据失败！长度:{0} \r\n{1}", data.Length, ex);
                                _tcpMapHelper[tcpClient, true] = null;
                            }
                        }
                    }
                    break;
                case P2PSocketType.Remote.ServerName.Code:
                    {
                        string key = data.ToStringUnicode();
                        _tcpMapHelper.SetControlClient(tcpClient, key);
                        Logger.Debug.WriteLine("[RemoteServerClient]->[服务器] 设置Remote服务名 ip:{0} key:{1}", tcpClient.Client.RemoteEndPoint, key);
                    }
                    break;
                case P2PSocketType.Remote.Transfer.Code:
                    {
                        if (toClient != null)
                        {
                            try
                            {
                                toClient.WriteAsync(data.ToArray(), P2PSocketType.Local.Code, P2PSocketType.Local.Transfer.Code);
                                Logger.Trace.WriteLine("[服务器]->[LocalServer] 发送Transfer数据，长度:{0}", data.Length);
                            }
                            catch (Exception ex)
                            {
                                Logger.Trace.WriteLine("[服务器]->[LocalServer] 发送Transfer数据失败！长度:{0} \r\n{1}", data.Length, ex);
                                _tcpMapHelper[tcpClient, true] = null;
                            }
                        }
                    }
                    break;
            }
        }
        public void CloseRelateTcp(TcpClient closedTcp)
        {
            TcpClient client = _tcpMapHelper[closedTcp, true];
            if (client != null)
            {
                try
                {
                    Logger.Debug.WriteLine("[服务器]->[LocalServer] 发送断开Local服务信号 {0}", client.Client.RemoteEndPoint);
                    client.WriteAsync(new byte[] { 0 }, P2PSocketType.Local.Code, P2PSocketType.Local.Break.Code);
                }
                catch { }
            }
            client = _tcpMapHelper[closedTcp, false];
            if (client != null)
            {
                try
                {
                    Logger.Debug.WriteLine("[服务器]->[LocalServer] 发送断开Remote服务信号 {0}", client.Client.RemoteEndPoint);
                    client.WriteAsync(new byte[] { 0 }, P2PSocketType.Remote.Code, P2PSocketType.Remote.Break.Code);
                }
                catch { }
            }
        }
    }
}
