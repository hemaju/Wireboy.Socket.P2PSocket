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
using System.Threading.Tasks;

namespace P2PSocket.Client
{
    public class P2PClient
    {
        AppCenter appCenter { set; get; }
        TcpCenter tcpCenter { set; get; }
        public P2PClient()
        {
            appCenter = EasyInject.Get<AppCenter>();
            tcpCenter = EasyInject.Get<TcpCenter>();
        }

        internal void ConnectServer()
        {
            try
            {
                tcpCenter.P2PServerTcp = new P2PTcpClient(appCenter.Config.ServerAddress, appCenter.Config.ServerPort);
            }
            catch
            {
                LogUtils.Error($"连接服务器失败:{appCenter.Config.ServerAddress}:{appCenter.Config.ServerPort}");
                return;
            }
            LogUtils.Info($"已连接服务器:{appCenter.Config.ServerAddress}:{appCenter.Config.ServerPort}", false);
            tcpCenter.P2PServerTcp.IsAuth = true;
            //向服务器发送客户端信息
            InitServerInfo(tcpCenter.P2PServerTcp);
            //监听来自服务器的消息
            Global_Func.ListenTcp<ReceivePacket>(tcpCenter.P2PServerTcp);
        }

        /// <summary>
        /// 定时发送心跳包，检测服务器连接状态
        /// </summary>
        internal void TestAndReconnectServer()
        {
            Guid curGuid = appCenter.CurrentGuid;
            while (true)
            {
                Thread.Sleep(5000);
                if (curGuid != appCenter.CurrentGuid) break;
                if (tcpCenter.P2PServerTcp != null)
                {
                    try
                    {
                        byte[] dataAr = new Send_0x0052().PackData();
                        tcpCenter.P2PServerTcp.GetStream().WriteAsync(dataAr, 0, dataAr.Length);
                    }
                    catch (Exception ex)
                    {
                        LogUtils.Warning($"服务器连接已被断开");
                        tcpCenter.P2PServerTcp = null;
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
            if (string.IsNullOrWhiteSpace(appCenter.Config.ClientName))
            {
                Send_0x0104 sendPacket = new Send_0x0104();
                byte[] dataAr = sendPacket.PackData();
                tcpClient.GetStream().WriteAsync(dataAr, 0, dataAr.Length);
            }
            else
            {
                Send_0x0101 sendPacket = new Send_0x0101();
                LogUtils.Info($"客户端名称：{appCenter.Config.ClientName}");
                byte[] dataAr = sendPacket.PackData();
                tcpClient.GetStream().WriteAsync(dataAr, 0, dataAr.Length);
            }
        }


        /// <summary>
        ///     监听映射端口
        /// </summary>
        internal void StartPortMap()
        {
            if (appCenter.Config.PortMapList.Count == 0) return;
            foreach (PortMapItem item in appCenter.Config.PortMapList)
            {
                ListenPortMap(item);
            }
        }

        /// <summary>
        /// 监听指定类型端口映射
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        internal bool ListenPortMap(PortMapItem item)
        {

            if (item.MapType == PortMapType.ip)
                return ListenPortMapPort_Ip(item);
            else
                return ListenPortMapPort_Server(item);
        }

        /// <summary>
        /// 添加/修改指定端口映射
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool UsePortMapItem(PortMapItem item)
        {
            if (tcpCenter.ListenerList.Any(t => t.Key.Item2 == item.LocalPort))
            {
                List<(string, int)> keys = new List<(string, int)>();
                tcpCenter.ListenerList.Where(t => t.Key.Item2 == item.LocalPort).ToList().ForEach(t => { t.Value.Stop(); keys.Add(t.Key); });
                keys.ForEach(key => tcpCenter.ListenerList.Remove(key));
                appCenter.Config.PortMapList = appCenter.Config.PortMapList.Where(t => t.LocalPort != item.LocalPort).ToList();
            }
            return ListenPortMap(item);
        }

        /// <summary>
        /// 移除指定端口映射
        /// </summary>
        /// <param name="localPort"></param>
        /// <returns></returns>
        public bool UnUsePortMapItem(int localPort)
        {
            //如果存在，则移除端口监听集合并停止监听
            if (tcpCenter.ListenerList.Any(t => t.Key.Item2 == localPort))
            {
                List<(string, int)> keys = new List<(string, int)>();
                tcpCenter.ListenerList.Where(t => t.Key.Item2 == localPort).ToList().ForEach(t => { t.Value.Stop(); keys.Add(t.Key); });
                keys.ForEach(key => tcpCenter.ListenerList.Remove(key));
            }
            //更新端口监听集合
            appCenter.Config.PortMapList = appCenter.Config.PortMapList.Where(t => t.LocalPort != localPort).ToList();
            return true;
        }

        /// <summary>
        /// 监听需要服务端的端口映射
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool ListenPortMapPort_Server(PortMapItem item)
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
                return false;
            }
            tcpCenter.ListenerList.Add((item.LocalAddress, item.LocalPort), listener);
            LogUtils.Info($"端口映射：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}", false);

            ListenSt listenSt = new ListenSt();
            listenSt.listener = listener;
            listenSt.item = item;
            listener.BeginAcceptSocket(AcceptSocket_Server, listenSt);
            return true;

        }

        /// <summary>
        /// 端口映射监听端口的新tcp回调方法
        /// </summary>
        /// <param name="ar"></param>
        public void AcceptSocket_Server(IAsyncResult ar)
        {
            ListenSt st = (ListenSt)ar.AsyncState;
            TcpListener listener = st.listener;
            PortMapItem item = st.item;
            Socket socket = null;
            try
            {
                //获取当前接入的tcp
                socket = listener.EndAcceptSocket(ar);
            }
            catch (Exception ex)
            {
                LogUtils.Error(ex.ToString());
                return;
            }
            try
            {
                listener.BeginAcceptSocket(AcceptSocket_Server, st);
            }
            catch (Exception ex)
            {
                LogUtils.Error("监听端口发生错误：" + listener.LocalEndpoint.ToString() + Environment.NewLine + ex.ToString());
            }
            try
            {
                if (tcpCenter.P2PServerTcp != null && tcpCenter.P2PServerTcp.Connected)
                {
                    P2PTcpClient tcpClient = new P2PTcpClient(socket);
                    //加入待连接集合
                    tcpCenter.WaiteConnetctTcp.Add(tcpClient.Token, tcpClient);
                    //发送消息给服务端，开始内网穿透
                    Send_0x0201_Apply packet = new Send_0x0201_Apply(tcpClient.Token, item.RemoteAddress, item.RemotePort, item.P2PType);
                    LogUtils.Debug(string.Format("正在建立{0}隧道 token:{1} client:{2} port:{3}", item.P2PType == 0 ? "中转模式" : "P2P模式", tcpClient.Token, item.RemoteAddress, item.RemotePort));

                    byte[] dataAr = packet.PackData();
                    EasyOp.Do(() =>
                    {
                        tcpCenter.P2PServerTcp.BeginSend(dataAr);
                    }, () =>
                    {
                        //等待指定超时时间后，判断是否连接成功
                        Thread.Sleep(AppConfig.P2PTimeout);
                        if (tcpCenter.WaiteConnetctTcp.ContainsKey(tcpClient.Token))
                        {
                            LogUtils.Debug($"建立隧道失败：token:{tcpClient.Token} {item.LocalPort}->{item.RemoteAddress}:{item.RemotePort} {AppConfig.P2PTimeout / 1000}秒无响应，已超时.");
                            tcpCenter.WaiteConnetctTcp[tcpClient.Token]?.SafeClose();
                            tcpCenter.WaiteConnetctTcp.Remove(tcpClient.Token);
                        }
                    }, ex =>
                    {
                        EasyOp.Do(tcpClient.SafeClose);
                        LogUtils.Debug($"建立隧道失败,无法连接服务器：token:{tcpClient.Token} {item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}.");
                    });
                }
                else
                {
                    LogUtils.Debug($"建立隧道失败：未连接到服务器!");
                    socket.Close();
                }
            }
            catch (Exception ex)
            {
                LogUtils.Debug("处理新tcp连接时发生错误：" + Environment.NewLine + ex.ToString());
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
        private bool ListenPortMapPort_Ip(PortMapItem item)
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
                return false;
            }
            tcpCenter.ListenerList.Add((item.LocalAddress, item.LocalPort), listener);
            LogUtils.Info($"添加端口映射成功：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}", false);

            ListenSt listenSt = new ListenSt();
            listenSt.listener = listener;
            listenSt.item = item;
            listener.BeginAcceptSocket(AcceptSocket_Ip, listenSt);
            return true;
        }

        /// <summary>
        /// 直接转发类型的端口监听，接收新tcp回调方法
        /// </summary>
        /// <param name="ar"></param>
        public void AcceptSocket_Ip(IAsyncResult ar)
        {
            ListenSt st = (ListenSt)ar.AsyncState;
            TcpListener listener = st.listener;
            PortMapItem item = st.item;
            Socket socket = null;
            try
            {
                socket = listener.EndAcceptSocket(ar);
            }
            catch (Exception ex)
            {
                LogUtils.Error(ex.ToString());
                return;
            }
            listener.BeginAcceptSocket(AcceptSocket_Ip, st);
            P2PTcpClient tcpClient = new P2PTcpClient(socket);
            P2PTcpClient ipClient = null;
            try
            {
                ipClient = new P2PTcpClient(item.RemoteAddress, item.RemotePort);
            }
            catch (Exception ex)
            {
                tcpClient?.SafeClose();
                LogUtils.Debug($"建立隧道失败：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}{Environment.NewLine}{ex}");
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
