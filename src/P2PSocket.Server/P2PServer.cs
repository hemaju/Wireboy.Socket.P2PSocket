using P2PSocket.Core;
using P2PSocket.Core.Commands;
using P2PSocket.Core.Extends;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using P2PSocket.Server.Commands;
using P2PSocket.Server.Models;
using P2PSocket.Server.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace P2PSocket.Server
{
    public class P2PServer
    {
        public List<TcpListener> ListenerList { set; get; } = new List<TcpListener>();
        public P2PServer()
        {

        }

        /// <summary>
        ///     启动服务
        /// </summary>
        public void StartServer()
        {
            ListenMessagePort();
            StartPortMap();
        }


        /// <summary>
        ///     监听映射端口
        /// </summary>
        private void StartPortMap()
        {
            foreach (PortMapItem item in ConfigCenter.Instance.PortMapList)
            {
                if (item.MapType == PortMapType.ip)
                {
                    ListenPortMapPortWithIp(item);
                }
                else
                {
                    ListenPortMapPortWithServerName(item);
                }
            }
        }

        /// <summary>
        ///     监听消息端口
        /// </summary>
        private void ListenMessagePort()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, ConfigCenter.Instance.LocalPort);
            try
            {
                listener.Start();
                LogUtils.Info($"【成功】启动服务，端口：{ConfigCenter.Instance.LocalPort}");
            }
            catch
            {
                LogUtils.Error($"【失败】服务端口：{ConfigCenter.Instance.LocalPort} 监听失败.");
                return;
            }
            ListenerList.Add(listener);
            ListenSt listenSt = new ListenSt();
            listenSt.listener = listener;
            listener.BeginAcceptSocket(AcceptSocket_Client, listenSt);
        }
        struct ListenSt
        {
            public TcpListener listener;
            public PortMapItem item;
        }

        public void AcceptSocket_Client(IAsyncResult ar)
        {
            ListenSt st = (ListenSt)ar.AsyncState;
            TcpListener listener = st.listener;
            Socket socket = listener.EndAcceptSocket(ar);
            listener.BeginAcceptSocket(AcceptSocket_Client, st);
            P2PTcpClient tcpClient = new P2PTcpClient(socket);
            LogUtils.Info($"端口{ ConfigCenter.Instance.LocalPort}新连入Tcp：{tcpClient.Client.RemoteEndPoint.ToString()}");
            //接收数据
            Global_Func.ListenTcp<ReceivePacket>(tcpClient);
        }

        private void ListenPortMapPortWithServerName(PortMapItem item)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, item.LocalPort);
            try
            {
                listener.Start();
            }
            catch (Exception ex)
            {
                LogUtils.Error($"【失败】端口映射：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}{Environment.NewLine}{ex.ToString()}");
                return;
            }
            ListenerList.Add(listener);
            LogUtils.Info($"【成功】端口映射：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}", false);

            ListenSt listenSt = new ListenSt();
            listenSt.listener = listener;
            listenSt.item = item;
            listener.BeginAcceptSocket(AcceptSocket_ClientName, listenSt);
        }

        public void AcceptSocket_ClientName(IAsyncResult ar)
        {
            ListenSt st = (ListenSt)ar.AsyncState;
            TcpListener listener = st.listener;
            PortMapItem item = st.item;
            Socket socket = listener.EndAcceptSocket(ar);
            listener.BeginAcceptSocket(AcceptSocket_ClientName, ar);

            string remoteAddress = ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();
            LogUtils.Info($"开始内网穿透：{remoteAddress}->{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}", false);
            P2PTcpClient tcpClient = new P2PTcpClient(socket);
            string token = tcpClient.Token;
            //获取目标tcp
            if (ClientCenter.Instance.TcpMap.ContainsKey(item.RemoteAddress) && ClientCenter.Instance.TcpMap[item.RemoteAddress].TcpClient.Connected)
            {
                //加入待连接集合
                ClientCenter.Instance.WaiteConnetctTcp.Add(token, tcpClient);
                //发送p2p申请
                Models.Send.Send_0x0211 packet = new Models.Send.Send_0x0211(token, item.RemotePort, tcpClient.RemoteEndPoint);
                ClientCenter.Instance.TcpMap[item.RemoteAddress].TcpClient.Client.Send(packet.PackData());
                AppCenter.Instance.StartNewTask(() =>
                {
                    Thread.Sleep(ConfigCenter.Instance.P2PTimeout);
                    //如果5秒后没有匹配成功，则关闭连接
                    if (ClientCenter.Instance.WaiteConnetctTcp.ContainsKey(token))
                    {
                        LogUtils.Warning($"【失败】内网穿透：{ConfigCenter.Instance.P2PTimeout / 1000}秒无响应，已超时.");
                        ClientCenter.Instance.WaiteConnetctTcp[token].SafeClose();
                        ClientCenter.Instance.WaiteConnetctTcp.Remove(token);
                    }
                });
            }
            else
            {
                LogUtils.Warning($"【失败】内网穿透：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort} 客户端不在线!");
                tcpClient.SafeClose();
            }


        }
        /// <summary>
        ///     直接转发类型的端口监听
        /// </summary>
        /// <param name="item"></param>
        private void ListenPortMapPortWithIp(PortMapItem item)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, item.LocalPort);
            try
            {
                listener.Start();
            }
            catch (Exception ex)
            {
                LogUtils.Error($"【失败】端口映射：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}{Environment.NewLine}{ex.ToString()}");
                return;
            }
            ListenerList.Add(listener);
            LogUtils.Info($"【成功】端口映射：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}", false);


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
            listener.BeginAcceptSocket(AcceptSocket_Ip, st);

            P2PTcpClient tcpClient = new P2PTcpClient(socket);
            P2PTcpClient ipClient = null;
            try
            {
                ipClient = new P2PTcpClient(item.RemoteAddress, item.RemotePort);
            }
            catch (Exception ex)
            {
                tcpClient.SafeClose();
                LogUtils.Error($"【失败】内网穿透：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}{Environment.NewLine}{ex.ToString()}");
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
