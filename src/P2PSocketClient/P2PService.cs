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
        TcpClient _clientTcp = null;
        object _lockClientTcp = new object();
        bool ClientTcpIsNull { get { return _clientTcp == null; } }
        TcpClient ClientTcp
        {
            set { _clientTcp = value; }
            get
            {
                if (_clientTcp == null && IsEnableHome)
                    lock (_lockClientTcp)
                    {
                        try
                        {
                            if (_clientTcp == null) _clientTcp = new TcpClient("127.0.0.1", ConfigServer.AppSettings.LocalHomePort);
                            _taskFactory.StartNew(() => { ListenClientServerPort(); });
                        }
                        catch (Exception ex)
                        {
                            Logger.Write("连接本地服务失败：{0}", ex);
                        }
                    }
                return _clientTcp;
            }
        }
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
                _taskFactory.StartNew(() => {
                    while (true)
                    {
                        if (ServerTcp == null)
                        {
                            try
                            {
                                //连接服务器
                                ServerTcp = new TcpClient(ConfigServer.AppSettings.ServerIp, ConfigServer.AppSettings.ServerPort);
                                _taskFactory.StartNew(() => { RecieveServerTcp(); });
                            }
                            catch (Exception ex)
                            {
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
                ConfigServer.AppSettings.HomeServerName = homeName;
                //发送Home服务名称
                ServerTcp.WriteAsync(homeName, MsgType.本地服务名);
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
                        Logger.Write("正在监听本地端口：{0}", ConfigServer.AppSettings.LocalClientPort);
                        TcpListener tcpListener = new TcpListener(IPAddress.Any, ConfigServer.AppSettings.LocalClientPort);
                        tcpListener.Start();
                        while (IsEnableClient)
                        {
                            TcpClient tcpClient = tcpListener.AcceptTcpClient();
                            if (ClientTcpIsNull && !ClientTcp.Connected)
                            {
                                ClientTcp = tcpClient;
                                _taskFactory.StartNew(() => { ListenClientServerPort(); });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Write("监听本地端口失败：{0}", ex);
                    }
                });
            }
        }

        /// <summary>
        /// 接收服务器数据
        /// </summary>
        public void RecieveServerTcp()
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

        /// <summary>
        /// 接收服务器数据回调方法
        /// </summary>
        /// <param name="asyncResult"></param>
        public void DoRecieveClientTcp(TcpResult tcpResult, int length)
        {
            if (length > 0)
            {
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
                case (byte)MsgType.数据转发:
                    {
                        if (!ClientTcpIsNull)
                        {
                            try
                            {
                                ClientTcp.WriteAsync(data.Skip(1).ToArray(), MsgType.不封包);
                            }
                            catch (Exception ex)
                            {
                                ClientTcp = null;
                                SendSocketBreak(ServerTcp);
                                Logger.Write("向本地端口发送数据错误：{0}", ex);
                            }
                        }
                    }
                    break;
                case (byte)MsgType.连接断开:
                    {
                        try
                        {
                            if (!ClientTcpIsNull)
                                ClientTcp.Close();
                            ClientTcp = null;
                        }
                        catch (Exception ex)
                        {
                            ClientTcp = null;
                        }
                    }
                    break;
                case (byte)MsgType.测试服务器:
                    {
                        string str = Encoding.Unicode.GetString(data.Skip(1).ToArray());
                        Console.WriteLine("测试数据：{0}",str);
                    }
                    break;
            }
        }
        /// <summary>
        /// 监听Client服务端口
        /// </summary>
        public void ListenClientServerPort()
        {
            NetworkStream readStream = ClientTcp.GetStream();
            TcpResult tcpResult = new TcpResult(readStream, ClientTcp, null);
            while (true)
            {
                int length = readStream.Read(tcpResult.Readbuffer, 0, tcpResult.Readbuffer.Length);
                DoRecieveClientServerPort(tcpResult, length);
                tcpResult.ResetReadBuffer();
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
                    ServerTcp.WriteAsync(tcpResult.Readbuffer, MsgType.数据转发);
                }
                catch (Exception ex)
                {
                    Logger.Write("向服务器发送数据错误：{0}", ex);
                    ServerTcp = null;
                    try
                    {
                        ClientTcp.Close();
                    }
                    catch (Exception ex1)
                    {

                    }
                    Logger.Write("断开本地其它服务Tcp");
                    ClientTcp = null;
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
