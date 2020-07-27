using P2PSocket.Client.Models.Send;
using P2PSocket.Client.Utils;
using P2PSocket.Core;
using P2PSocket.Core.Extends;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace P2PSocket.Client
{
    public class P2PClient
    {
        public P2PClient()
        {

        }

        internal void ConnectServer()
        {
            try
            {
                TcpCenter.Instance.P2PServerTcp = new P2PTcpClient(ConfigCenter.Instance.ServerAddress, ConfigCenter.Instance.ServerPort);
            }
            catch
            {
                LogUtils.Error($"{DateTime.Now.ToString("HH:mm:ss")} 无法连接服务器:{ConfigCenter.Instance.ServerAddress}:{ConfigCenter.Instance.ServerPort}");
                return;
            }
            LogUtils.Info($"{DateTime.Now.ToString("HH:mm:ss")} 已连接服务器:{ConfigCenter.Instance.ServerAddress}:{ConfigCenter.Instance.ServerPort}", false);
            TcpCenter.Instance.P2PServerTcp.IsAuth = true;
            //向服务器发送客户端信息
            InitServerInfo(TcpCenter.Instance.P2PServerTcp);
            //监听来自服务器的消息
            Global_Func.ListenTcp<ReceivePacket>(TcpCenter.Instance.P2PServerTcp);
        }

        internal void TestAndReconnectServer()
        {
            Guid curGuid = AppCenter.Instance.CurrentGuid;
            while (true)
            {
                Thread.Sleep(5000);
                if (curGuid != AppCenter.Instance.CurrentGuid) break;
                if (TcpCenter.Instance.P2PServerTcp != null)
                {
                    try
                    {
                        byte[] dataAr = new Send_0x0052().PackData();
                        TcpCenter.Instance.P2PServerTcp.GetStream().WriteAsync(dataAr, 0, dataAr.Length);
                    }
                    catch (Exception ex)
                    {
                        LogUtils.Warning($"{DateTime.Now.ToString("HH:mm:ss")} 服务器连接已被断开");
                        TcpCenter.Instance.P2PServerTcp = null;
                    }
                }
                else
                {
                    ConnectServer();
                }
            }
        }

        /// <summary>
        ///     向服务器发送客户端信息
        /// </summary>
        /// <param name="tcpClient"></param>
        private void InitServerInfo(P2PTcpClient tcpClient)
        {
            if (string.IsNullOrWhiteSpace(ConfigCenter.Instance.ClientName))
            {
                Send_0x0104 sendPacket = new Send_0x0104();
                byte[] dataAr = sendPacket.PackData();
                tcpClient.GetStream().WriteAsync(dataAr, 0, dataAr.Length);
            }
            else
            {
                Send_0x0101 sendPacket = new Send_0x0101();
                LogUtils.Info($"客户端名称：{ConfigCenter.Instance.ClientName}");
                byte[] dataAr = sendPacket.PackData();
                tcpClient.GetStream().WriteAsync(dataAr, 0, dataAr.Length);
            }
        }


        /// <summary>
        ///     监听映射端口
        /// </summary>
        internal void StartPortMap()
        {
            if (ConfigCenter.Instance.PortMapList.Count == 0) return;
            Dictionary<string, TcpListener> curListenerList = TcpCenter.Instance.ListenerList;
            TcpCenter.Instance.ListenerList = new Dictionary<string, TcpListener>();
            foreach (PortMapItem item in ConfigCenter.Instance.PortMapList)
            {
                string key = $"{item.LocalAddress}:{item.LocalPort}";
                if (curListenerList.ContainsKey(key))
                {
                    LogUtils.Trace($"正在监听端口：{key}");
                    TcpCenter.Instance.ListenerList.Add(key, curListenerList[key]);
                    curListenerList.Remove(key);
                    continue;
                }
                if (item.MapType == PortMapType.ip)
                {
                    ListenPortMapPort_Ip(item);
                }
                else
                {
                    ListenPortMapPort_Server(item);
                }
            }
            foreach (TcpListener listener in curListenerList.Values)
            {
                LogUtils.Trace($"停止端口监听：{listener.LocalEndpoint.ToString()}");
                listener.Stop();
            }
        }

        private void ListenPortMapPort_Server(PortMapItem item)
        {
            TcpListener listener = null;
            try
            {
                listener = new TcpListener(string.IsNullOrEmpty(item.LocalAddress) ? IPAddress.Any : IPAddress.Parse(item.LocalAddress), item.LocalPort);
                listener.Start();
            }
            catch (SocketException ex)
            {
                LogUtils.Error($"端口映射失败：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}{Environment.NewLine}{ex.Message}");
                return;
            }
            TcpCenter.Instance.ListenerList.Add($"{item.LocalAddress}:{item.LocalPort}", listener);
            LogUtils.Info($"端口映射：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}", false);

            ListenSt listenSt = new ListenSt();
            listenSt.listener = listener;
            listenSt.item = item;
            listener.BeginAcceptSocket(AcceptSocket_Server, listenSt);

        }

        public void AcceptSocket_Server(IAsyncResult ar)
        {
            ListenSt st = (ListenSt)ar.AsyncState;
            TcpListener listener = st.listener;
            PortMapItem item = st.item;
            Socket socket = listener.EndAcceptSocket(ar);
            listener.BeginAcceptSocket(AcceptSocket_Server, st);
            if (TcpCenter.Instance.P2PServerTcp != null && TcpCenter.Instance.P2PServerTcp.Connected)
            {
                P2PTcpClient tcpClient = new P2PTcpClient(socket);
                //加入待连接集合
                TcpCenter.Instance.WaiteConnetctTcp.Add(tcpClient.Token, tcpClient);
                //发送p2p申请
                Send_0x0201_Apply packet = new Send_0x0201_Apply(tcpClient.Token, item.RemoteAddress, item.RemotePort, item.P2PType);
                LogUtils.Info(string.Format("正在建立{0}隧道 token:{1} client:{2} port:{3}", item.P2PType == 0 ? "中转模式" : "P2P模式", tcpClient.Token, item.RemoteAddress, item.RemotePort));
                try
                {
                    byte[] dataAr = packet.PackData();
                    TcpCenter.Instance.P2PServerTcp.GetStream().WriteAsync(dataAr, 0, dataAr.Length);
                }
                finally
                {
                    //如果5秒后没有匹配成功，则关闭连接
                    Thread.Sleep(ConfigCenter.P2PTimeout);
                    if (TcpCenter.Instance.WaiteConnetctTcp.ContainsKey(tcpClient.Token))
                    {
                        LogUtils.Info($"建立隧道失败：token:{tcpClient.Token} {item.LocalPort}->{item.RemoteAddress}:{item.RemotePort} {ConfigCenter.P2PTimeout / 1000}秒无响应，已超时.");
                        TcpCenter.Instance.WaiteConnetctTcp[tcpClient.Token].SafeClose();
                        TcpCenter.Instance.WaiteConnetctTcp.Remove(tcpClient.Token);
                    }

                }
            }
            else
            {
                LogUtils.Warning($"建立隧道失败：未连接到服务器!");
                socket.Close();
            }
        }

        struct ListenSt
        {
            public TcpListener listener;
            public PortMapItem item;
        }

        /// <summary>
        ///     直接转发类型的端口监听
        /// </summary>
        /// <param name="item"></param>
        private void ListenPortMapPort_Ip(PortMapItem item)
        {
            TcpListener listener = null;
            try
            {
                listener = new TcpListener(string.IsNullOrEmpty(item.LocalAddress) ? IPAddress.Any : IPAddress.Parse(item.LocalAddress), item.LocalPort);
                listener.Start();
            }
            catch (Exception ex)
            {
                LogUtils.Error($"添加端口映射失败：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}{Environment.NewLine}{ex.ToString()}");
                return;
            }
            TcpCenter.Instance.ListenerList.Add($"{item.LocalAddress}:{item.LocalPort}", listener);
            LogUtils.Info($"添加端口映射成功：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}", false);

            ListenSt listenSt = new ListenSt();
            listenSt.listener = listener;
            listenSt.item = item;
            listener.BeginAcceptSocket(AcceptSocket_Ip, listenSt);
        }
        public void AcceptSocket_Ip(IAsyncResult ar)
        {
            ListenSt st = (ListenSt)ar.AsyncState;
            TcpListener listener = st.listener;
            PortMapItem item = st.item;
            Socket socket = listener.EndAcceptSocket(ar);
            listener.BeginAcceptSocket(AcceptSocket_Server, st);
            P2PTcpClient tcpClient = new P2PTcpClient(socket);
            P2PTcpClient ipClient = null;
            try
            {
                ipClient = new P2PTcpClient(item.RemoteAddress, item.RemotePort);
            }
            catch (Exception ex)
            {
                tcpClient.SafeClose();
                LogUtils.Error($"建立隧道失败：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}{Environment.NewLine}{ex}");
            }
            if (ipClient.Connected)
            {
                tcpClient.ToClient = ipClient;
                ipClient.ToClient = tcpClient;
                RelationTcp toRelation = new RelationTcp();
                toRelation.readTcp = tcpClient;
                toRelation.readSs = tcpClient.GetStream();
                toRelation.writeTcp = tcpClient.ToClient;
                toRelation.writeSs = tcpClient.ToClient.GetStream();
                toRelation.buffer = new byte[P2PGlobal.P2PSocketBufferSize];
                RelationTcp fromRelation = new RelationTcp();
                fromRelation.readTcp = toRelation.writeTcp;
                fromRelation.readSs = toRelation.writeSs;
                fromRelation.writeTcp = toRelation.readTcp;
                fromRelation.writeSs = toRelation.readSs;
                fromRelation.buffer = new byte[P2PGlobal.P2PSocketBufferSize];
                StartTransferTcp_Ip(toRelation);
                StartTransferTcp_Ip(fromRelation);
            }
        }

        private void StartTransferTcp_Ip(RelationTcp tcp)
        {
            tcp.readTcp.GetStream().BeginRead(tcp.buffer, 0, tcp.buffer.Length, TransferTcp_Ip, tcp);
        }
        private void TransferTcp_Ip(IAsyncResult ar)
        {
            RelationTcp relation = (RelationTcp)ar.AsyncState;

            if (relation.readSs.CanRead)
            {
                int length = 0;
                try
                {
                    length = relation.readSs.EndRead(ar);
                }
                catch { }
                if (length > 0)
                {
                    if (relation.writeSs.CanWrite)
                    {
                        try
                        {
                            relation.writeSs.Write(relation.buffer.Take(length).ToArray(), 0, length);
                            StartTransferTcp_Ip(relation);
                            return;
                        }
                        catch { }
                    }
                }
            }
            relation.readSs.Close(3000);
            relation.writeSs.Close(3000);
            relation.readTcp.Close();
            relation.writeTcp.Close();
        }
        public struct RelationTcp
        {
            public TcpClient readTcp;
            public TcpClient writeTcp;
            public NetworkStream readSs;
            public NetworkStream writeSs;
            public byte[] buffer;
        }
    }
}
