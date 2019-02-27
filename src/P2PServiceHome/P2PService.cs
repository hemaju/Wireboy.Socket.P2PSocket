using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using Wireboy.Socket.P2PHome;
using Wireboy.Socket.P2PHome.Services;
using Wireboy.Socket.P2PHome.Models;

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
        TcpClient _localTcp = null;
        object _lockLocalTcp = new object();
        TcpClient LocalTcp
        {
            set { _localTcp = value; }
            get
            {
                if (_localTcp == null)
                    lock (_lockLocalTcp)
                    {
                        try
                        {
                            if (_localTcp == null) _localTcp = new TcpClient("127.0.0.1", ConfigServer.AppSettings.LocalPort);
                            _taskFactory.StartNew(() => { ListenLocalPort(); });
                        }
                        catch (Exception ex)
                        {
                            Logger.Write("连接本地服务失败：{0}", ex);
                        }
                    }
                return _localTcp;
            }
        }
        /// <summary>
        /// 服务器Tcp连接
        /// </summary>
        TcpClient ServerTcp { set; get; } = null;
        public P2PService()
        {
        }

        /// <summary>
        /// 启动服务
        /// </summary>
        public void Start()
        {
            while (true)
            {
                if (ServerTcp == null)
                {
                    try
                    {
                        Console.WriteLine("正在连接服务器...");
                        ServerTcp = new TcpClient(ConfigServer.AppSettings.ServerIp, ConfigServer.AppSettings.ServerPort);
                        _taskFactory.StartNew(() => { RecieveServerTcp(); });

                        if (ServerTcp != null && ServerTcp.Connected)
                        {
                            Console.WriteLine("成功连接服务器！");
                            while (string.IsNullOrEmpty(ConfigServer.AppSettings.ServerName))
                            {
                                Console.WriteLine("请输入服务名称：");
                                ConfigServer.AppSettings.ServerName = Console.ReadLine();
                            }
                            Console.WriteLine(string.Format("当前服务名称：{0}", ConfigServer.AppSettings.ServerName));
                            Logger.Write("当前服务名称：{0}", ConfigServer.AppSettings.ServerName);
                            try
                            {
                                ServerTcp.WriteAsync(Encoding.ASCII.GetBytes(ConfigServer.AppSettings.ServerName), MsgType.本地服务名);
                            }
                            catch (Exception ex)
                            {
                                ServerTcp = null;
                                Logger.Write("{0}", ex);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("{0}", ex);
                        Logger.Write("{0}", ex);
                        ServerTcp = null;
                    }
                }
                else
                {
                    try
                    {
                        ServerTcp.WriteAsync(new byte[] { 0 }, MsgType.心跳包);
                    }
                    catch (Exception ex)
                    {
                        ServerTcp = null;
                        Logger.Write("发送心跳包失败：{0}", ex);
                    }
                }
                Thread.Sleep(1000);
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
                        if (LocalTcp != null)
                        {
                            try
                            {
                                LocalTcp.WriteAsync(data.Skip(1).ToArray(), MsgType.不封包);
                            }
                            catch (Exception ex)
                            {
                                LocalTcp = null;
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
                            if (LocalTcp != null)
                                LocalTcp.Close();
                            LocalTcp = null;
                        }
                        catch (Exception ex)
                        {
                            LocalTcp = null;
                        }
                    }
                    break;
            }
        }
        /// <summary>
        /// 监听本地端口
        /// </summary>
        public void ListenLocalPort()
        {
            NetworkStream readStream = LocalTcp.GetStream();
            TcpResult tcpResult = new TcpResult(readStream, LocalTcp, null);
            while (true)
            {
                int length = readStream.Read(tcpResult.Readbuffer, 0, tcpResult.Readbuffer.Length);
                DoRecieveLocalClientTcp(tcpResult, length);
                tcpResult.ResetReadBuffer();
            }
        }

        /// <summary>
        /// 本地端口数据接收回调
        /// </summary>
        /// <param name="asyncResult"></param>
        public void DoRecieveLocalClientTcp(TcpResult tcpResult, int length)
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
                        LocalTcp.Close();
                    }
                    catch(Exception ex1)
                    {

                    }
                    Logger.Write("断开本地服务Tcp");
                    LocalTcp = null;
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
    }
}
