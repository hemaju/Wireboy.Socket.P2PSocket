using P2PSocektLib.Command;
using P2PSocektLib.Enum;
using P2PSocektLib.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace P2PSocektLib.Export
{
    internal class P2PServer : IP2PServer
    {
        /// <summary>
        /// 服务端口
        /// </summary>
        int Port { set; get; }
        P2PListener? Listener { set; get; }
        /// <summary>
        /// 端口映射字典
        /// </summary>
        internal Dictionary<int, PortMapItem> PortMap { set; get; }
        /// <summary>
        /// 端口监听字典
        /// </summary>
        internal Dictionary<int, P2PListener> ListenerMap { set; get; }
        /// <summary>
        /// 命令处理字典
        /// </summary>
        internal Dictionary<RequestEnum, IServerExcute> ExcuteMap { set; get; }
        /// <summary>
        /// 命令请求实例
        /// </summary>
        internal RequestService Bus_Request { set; get; }
        /// <summary>
        /// 命令请求实例
        /// </summary>
        internal ResponseService Bus_Response { set; get; }
        /// <summary>
        /// 有效的Token信息
        /// </summary>
        internal ConcurrentBag<string> TokenList { set; get; }
        /// <summary>
        /// 管道字典
        /// </summary>
        ConcurrentDictionary<string, P2PPipe> PipeMap;
        /// <summary>
        /// 客户端连接字典
        /// </summary>
        ConcurrentDictionary<string, P2PConnect> ClientMap { set; get; }
        Utils_T_AsyncTask<string, P2PConnect> PipeCreatTask { set; get; }

        public P2PServer(int port)
        {
            Port = port;
            PortMap = new Dictionary<int, PortMapItem>();
            ExcuteMap = new Dictionary<RequestEnum, IServerExcute>();
            Bus_Request = new RequestService();
            Bus_Response = new ResponseService();
            TokenList = new ConcurrentBag<string>();
            ListenerMap = new Dictionary<int, P2PListener>();
            PipeMap = new ConcurrentDictionary<string, P2PPipe>();
            ClientMap = new ConcurrentDictionary<string, P2PConnect>();
            PipeCreatTask = new Utils_T_AsyncTask<string, P2PConnect>();
            InitExcute();
        }
        /// <summary>
        /// 初始化命令处理方法
        /// </summary>
        private void InitExcute()
        {
            // 通过反射初始化命令处理类
            Type[] commandList = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetCustomAttribute<ServerExcuteAttribute>() != null)
                .ToArray();
            foreach (Type type in commandList)
            {
                ServerExcuteAttribute? attr = type.GetCustomAttribute<ServerExcuteAttribute>();
                if (attr != null)
                {
                    IServerExcute? handle = Activator.CreateInstance(type) as IServerExcute;
                    if (handle != null) ExcuteMap.Add(attr.RequestType, handle);
                }

            }
        }

        /// <summary>
        /// 创建客户端用于登录的Token
        /// </summary>
        /// <returns></returns>
        internal string NewClientToken()
        {
            string nToken;
            do
            {
                nToken = Guid.NewGuid().ToString();
            } while (TokenList.Contains(nToken));
            return nToken;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void RemovePortMapItem(int localPort)
        {
            throw new NotImplementedException();
        }

        public void StartListen()
        {
            if (Listener != null) Listener.Stop();
            Listener = new P2PListener(Port);
            Listener.Start();
            Listener.BindAcceptConnectionEvent(ServerListenAcceptConnect);
        }

        private async void ServerListenAcceptConnect(INetworkConnect conn)
        {
            P2PConnect clientConn = new P2PConnect(conn);
            CmdPacket packet = new CmdPacket(conn);
            try
            {
                while (true)
                {
                    byte[] buffer = await packet.ReadOne();
                    if (ExcuteMap.ContainsKey(packet.RequestType))
                    {
                        await ExcuteMap[packet.RequestType].Handle(this, clientConn, buffer, packet.Token);
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        public void UpdatePortMapItem(PortMapItem item)
        {
            // 是否需要重新监听端口
            bool beginListenPort;
            // 更新端口映射表
            if (PortMap.ContainsKey(item.LocalPort))
            {
                beginListenPort = item.PortType != PortMap[item.LocalPort].PortType;
                PortMap[item.LocalPort] = item;
            }
            else
            {
                PortMap.Add(item.LocalPort, item);
                beginListenPort = true;
            }
            if (beginListenPort)
            {
                if (ListenerMap.ContainsKey(item.LocalPort))
                {
                    ListenerMap[item.LocalPort].Stop();
                    ListenerMap.Remove(item.LocalPort);
                }
                StartPortMapListen(item.LocalPort, item.PortType);
            }
        }

        private void StartPortMapListen(int port, NetworkType type)
        {
            P2PListener listener = new P2PListener(port, type);
            listener.Start();
            // 将监听加入字典，有用于监听管理
            ListenerMap.Add(port, listener);
            listener.BindAcceptConnectionEvent(conn =>
            {
                if (PortMap.ContainsKey(port))
                {
                    PortMapItem item = PortMap[port];
                    switch (item.ConnectType)
                    {
                        case P2PMode.IP直连:
                            {
                                Trasfer_IP(conn, item);
                                break;
                            }
                        case P2PMode.服务器中转:
                            {
                                Transfer_Server(conn, item);
                                break;
                            }
                        default:
                            throw new ArgumentException("不支持的类型");
                    }
                }
                else
                {
                    conn.Close();
                }
            });
        }
        #region IP直接转发
        private void Trasfer_IP(INetworkConnect conn, PortMapItem item)
        {
            P2PConnect toConn = new P2PConnect(item.PortType);
            toConn.Connect(item.RemoteAddress, item.RemotePort);
            Trasfer_TCP_Switch(toConn.Conn, conn);
            Trasfer_TCP_Switch(conn, toConn.Conn);
        }

        private async void Trasfer_TCP_Switch(INetworkConnect readConn, INetworkConnect writeConn)
        {
            byte[] buffer = new byte[1024];
            int length = 0;
            try
            {
                do
                {
                    length = await readConn.ReadData(buffer, 1024);
                    if (length != 0)
                    {
                        Console.WriteLine(Encoding.UTF8.GetString(buffer, 0, length));
                        try
                        {
                            await writeConn.SendData(buffer, length);
                        }
                        catch (Exception ex)
                        {
                            //readConn.Close();
                            break;
                        }
                    }
                    else
                    {
                        writeConn.Close();
                    }
                } while (length != 0);
            }
            catch (Exception ex)
            {
                writeConn.Close();
            }
        }
        #endregion

        #region 服务器中转
        private async Task Transfer_Server(INetworkConnect conn, PortMapItem item)
        {
            string pipeKey = $"{item.RemoteAddress}_{item.RemotePort}";
            P2PPipe? pipe = null;
            bool createPipe = true;
            //判断是否单通道
            if (item.IsSingle)
            {
                // 查询通道
                if (PipeMap.ContainsKey(pipeKey))
                {
                    pipe = PipeMap[pipeKey];
                    createPipe = false;
                }
                else createPipe = true;
            }
            // 创建管道
            if (createPipe)
                pipe = await CreateClientPipe(item.RemoteAddress);
            // 开始转发数据
            pipe.AddConnect(conn, item);
        }

        private async Task<P2PPipe> CreateClientPipe(string clientName)
        {
            if (ClientMap.ContainsKey(clientName))
            {
                var conn = ClientMap[clientName];
                // 设置管道的唯一识别码
                string token = Guid.NewGuid().ToString();
                ApiModel_NotifyCreatePipe model = new ApiModel_NotifyCreatePipe(token);
                // 等待用于管道的网络连接
                P2PConnect pipeConn = await PipeCreatTask.Wait(token, async () =>
                {
                    // 通知客户端建立新连接
                    await Bus_Request.NotifyCreatePipe(conn.SendData, model);
                }, TimeSpan.FromSeconds(5));
                P2PPipe pipe = new P2PPipe(clientName, pipeConn);
                // 打开管道
                await pipe.Open();
                return pipe;
            }
            else
            {
                throw new Exception("指定客户端不在线");
            }
        }
        #endregion
    }
}
