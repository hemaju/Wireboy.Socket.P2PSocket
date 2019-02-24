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

namespace Wireboy.Socket.P2PService
{
    public class P2PService
    {
        public TcpClientMapCollection<TcpClientMap> _tcpMapList = new TcpClientMapCollection<TcpClientMap>();
        private TaskFactory _taskFactory = new TaskFactory();
        public P2PService()
        {

        }

        public void Start()
        {
            //监听通讯端口
            _taskFactory.StartNew(() => { ListenServerPort(); });
            //监听转发端口
            ListenTransferPort();
        }

        /// <summary>
        /// 监听通讯端口
        /// </summary>
        public void ListenServerPort()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, ApplicationConfig.ServerPort);
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
        /// <summary>
        /// 监听数据转发端口
        /// </summary>
        public void ListenTransferPort()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, ApplicationConfig.TransferPort);
            while (true)
            {
                TcpClient tcpClient = listener.AcceptTcpClient();
                Logger.Write("转发端口：接收到来自{0}的tcp接入", tcpClient.Client.RemoteEndPoint);
                _taskFactory.StartNew(() =>
                {
                    //处理数据转发
                });
            }
        }

        public void RecieveClientTcp(TcpClient readTcp)
        {
            NetworkStream readStream = readTcp.GetStream();
            TcpResult tcpResult = new TcpResult(readStream, readTcp, ReievedTcpDataCallBack);
            while (readStream.CanRead)
            {
                IAsyncResult asyncResult = readStream.BeginRead(tcpResult.Readbuffer, 0, tcpResult.Readbuffer.Length, new AsyncCallback(DoRecieveClientTcp), tcpResult);
                tcpResult.ResetReadBuffer();
            }
        }
        ConcurrentDictionary<TcpClient, byte[]> _socketTempDic = new ConcurrentDictionary<TcpClient, byte[]>();
        public void DoRecieveClientTcp(IAsyncResult asyncResult)
        {
            TcpResult tcpResult = (TcpResult)asyncResult.AsyncState;
            int length = tcpResult.ReadStream.EndRead(asyncResult);
            if (length > 0)
            {
                int curReadIndex = 0;
                do
                {
                    tcpResult.ReadOnePackageData(length, ref curReadIndex);
                } while (curReadIndex <= length - 1);
            }
        }

        public void ReievedTcpDataCallBack(byte[] data,TcpResult tcpResult)
        {
            switch(data[0])
            {
                case 0:
                    //心跳包
                    ; break;
                case 1:
                    //账号密码
                    ; break;
                case 3:
                    //本地服务名称
                    ;break;
                case 5:
                    //要连接的服务名称
                    ;break;
                case 7:
                    //中断数据转发
                    ;break;
            }
        }
    }
}
