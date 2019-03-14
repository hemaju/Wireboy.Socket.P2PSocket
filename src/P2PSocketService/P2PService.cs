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
            catch(Exception ex)
            {
                Logger.Write("监听端口错误：{0}", ex);
                Thread.Sleep(2000);
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
            NetworkStream readStream = readTcp.GetStream();
            TcpResult tcpResult = new TcpResult(readStream, readTcp, ReievedTcpDataCallBack);
            while (readTcp.Connected)
            {
                try
                {
                   int length = readStream.Read(tcpResult.Readbuffer, 0, tcpResult.Readbuffer.Length);
                    DoRecieveClientTcp(tcpResult, length);
                    tcpResult.ResetReadBuffer();
                }catch(Exception ex)
                {
                    Logger.Write("P2PService -> RecieveClientTcp: {0}", ex);
                }
            }
        }
        public void DoRecieveClientTcp(TcpResult tcpResult,int length)
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

        public void ReievedTcpDataCallBack(byte[] data, TcpResult tcpResult)
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
                        _tcpMapHelper.SetHomeClient(tcpResult.ReadTcp, key);
                        Logger.Debug("设置本地Home服务名 ip:{0} key:{1}", tcpResult.ReadTcp.Client.RemoteEndPoint, key);
                    }
                    break;
                case (byte)MsgType.远程服务名:
                    {
                        string key = data.ToStringUnicode(1);
                        _tcpMapHelper.SetControlClient(tcpResult.ReadTcp, key);
                        Logger.Debug("设置远程Home服务名 ip:{0} key:{1}", tcpResult.ReadTcp.Client.RemoteEndPoint, key);
                    }
                    break;
                case (byte)MsgType.测试客户端:
                case (byte)MsgType.转发FromClient:
                case (byte)MsgType.转发FromHome:
                case (byte)MsgType.连接断开:
                    {
                        TcpClient toClient = _tcpMapHelper[tcpResult.ReadTcp];
                        if (toClient != null)
                        {
                            try
                            {
                                Logger.Debug("将数据转发到:{0}",toClient.Client.RemoteEndPoint);
                                toClient.WriteAsync(data.Skip(1).ToArray(), (MsgType)data[0]);
                            }
                            catch (Exception ex)
                            {
                                Logger.Write("数据转发异常：{0}", ex);
                                _tcpMapHelper[tcpResult.ReadTcp] = null;
                            }
                        }
                    }
                    break;
                case (byte)MsgType.测试服务器:
                    {
                        tcpResult.ReadTcp.WriteAsync(data.Skip(1).ToArray(),MsgType.测试服务器);
                    }
                    break;
            }
        }
    }
}
