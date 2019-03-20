/*
 * 主服务
 * 接收心跳包、客户端与服务器的数据交流
 * 
 */
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
        public P2PService()
        {

        }

        public void Start()
        {
            //监听通讯端口
            ListenServerPort();
        }

        /// <summary>
        /// 监听通讯端口
        /// </summary>
        public void ListenServerPort()
        {
            Logger.Write("监听本地端口：{0}", ConfigServer.AppSettings.ServerPort);
            TcpListener listener = null;
            try
            {
                listener = new TcpListener(IPAddress.Any, ConfigServer.AppSettings.ServerPort);
                listener.Start();
            }
            catch (Exception ex)
            {
                Logger.Write("监听端口错误：\r\n{0}", ex);
                return;
            }
            while (true)
            {
                TcpClient tcpClient = listener.AcceptTcpClient();
                Logger.Write("数据端口：接收到来自{0}的tcp接入", tcpClient.Client.RemoteEndPoint);
                _taskFactory.StartNew(() =>
                {
                    RecieveClientTcp(tcpClient);
                });
            }
        }

        public void RecieveClientTcp(TcpClient readTcp)
        {
            EndPoint endPoint = readTcp.Client.RemoteEndPoint;
            try
            {
                NetworkStream readStream = readTcp.GetStream();
                TcpHelper tcpHelper = new TcpHelper();
                byte[] buffer = new byte[1024];
                while (true)
                {
                    int length = readStream.Read(buffer, 0, buffer.Length);
                    ConcurrentQueue<byte[]> results = tcpHelper.ReadPackages(buffer, length);
                    while (!results.IsEmpty)
                    {
                        byte[] data;
                        if (results.TryDequeue(out data))
                        {
                            ReievedTcpDataCallBack(data, readTcp);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write("接收来自{0}的数据异常：\r\n{1} ", endPoint, ex);
            }
        }

        public void ReievedTcpDataCallBack(byte[] data, TcpClient tcpClient)
        {
            switch (data[0])
            {
                case (byte)MsgType.心跳包:
                    ; break;
                case (byte)MsgType.身份验证:
                    ; break;
                case (byte)MsgType.本地服务名:
                    {
                        string key = data.ToStringUnicode(1);
                        _tcpMapHelper.SetHomeClient(tcpClient, key);
                        Logger.Debug("设置本地Home服务名 ip:{0} key:{1}", tcpClient.Client.RemoteEndPoint, key);
                    }
                    break;
                case (byte)MsgType.远程服务名:
                    {
                        string key = data.ToStringUnicode(1);
                        _tcpMapHelper.SetControlClient(tcpClient, key);
                        Logger.Debug("设置远程Home服务名 ip:{0} key:{1}", tcpClient.Client.RemoteEndPoint, key);
                    }
                    break;
                case (byte)MsgType.测试客户端:
                case (byte)MsgType.转发FromClient:
                case (byte)MsgType.转发FromHome:
                case (byte)MsgType.断开FromHome:
                case (byte)MsgType.断开FromClient:
                    {
                        bool isFromClient = (data[0] == (byte)MsgType.转发FromClient || data[0] == (byte)MsgType.断开FromClient) ? true : false;
                        TcpClient toClient = _tcpMapHelper[tcpClient, isFromClient];
                        if (toClient != null)
                        {
                            try
                            {
                                Logger.Debug("将数据转发到:{0}", toClient.Client.RemoteEndPoint);
                                toClient.WriteAsync(data.ToArray(), MsgType.无类型);
                            }
                            catch (Exception ex)
                            {
                                Logger.Write("数据转发异常：{0}", ex);
                                _tcpMapHelper[tcpClient, true] = null;
                            }
                        }
                    }
                    break;
                case (byte)MsgType.测试服务器:
                    {
                        tcpClient.WriteAsync(data.Skip(1).ToArray(), MsgType.测试服务器);
                    }
                    break;
            }
        }
    }
}
