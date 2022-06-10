using P2PSocektLib.Command;
using P2PSocektLib.Enum;
using P2PSocektLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace P2PSocektLib.Export
{
    internal class P2PClient : IP2PClient
    {
        /// <summary>
        /// 服务器地址
        /// </summary>
        internal string Host { set; get; }
        /// <summary>
        /// 服务端口
        /// </summary>
        internal int Port { set; get; }
        /// <summary>
        /// 端口映射字典
        /// </summary>
        internal Dictionary<int, PortMapItem> PortMap { set; get; }
        /// <summary>
        /// 端口监听字典
        /// </summary>
        internal Dictionary<int, P2PListener> ListenerMap { set; get; }
        /// <summary>
        /// 客户端编码（客户端唯一编码）
        /// </summary>
        internal string ClientCode { get => clientCode; }
        /// <summary>
        /// 客户端编码（客户端唯一编码）
        /// </summary>
        internal string clientCode;
        /// <summary>
        /// 登录token
        /// </summary>
        internal string LoginToken { set; get; }
        /// <summary>
        /// 与服务端通讯的连接
        /// </summary>
        internal P2PConnect? ServerConn { set; get; }
        /// <summary>
        /// 命令处理字典
        /// </summary>
        internal Dictionary<RequestEnum, IClientExcute> ExcuteMap;
        /// <summary>
        /// 命令请求实例
        /// </summary>
        internal RequestService Bus { set; get; }

        public P2PClient(string address, int port)
        {
            Host = address;
            Port = port;
            PortMap = new Dictionary<int, PortMapItem>();
            ListenerMap = new Dictionary<int, P2PListener>();
            clientCode = "";
            ExcuteMap = new Dictionary<RequestEnum, IClientExcute>();
            Bus = new RequestService();
            InitExcute();
        }

        /// <summary>
        /// 初始化命令处理方法
        /// </summary>
        private void InitExcute()
        {
            // 通过反射初始化命令处理类
            Type[] commandList = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetCustomAttribute<ClientExcuteAttribute>() != null)
                .ToArray();
            foreach (Type type in commandList)
            {
                ClientExcuteAttribute? attr = type.GetCustomAttribute<ClientExcuteAttribute>();
                if (attr != null)
                {
                    IClientExcute? handle = Activator.CreateInstance(type) as IClientExcute;
                    if (handle != null) ExcuteMap.Add(attr.RequestType, handle);
                }

            }
        }

        public async Task ConnectServer()
        {
            // 多次调用时，先关闭原连接
            if (ServerConn != null)
                ServerConn.Close();
            // 与服务端建立tcp连接
            ServerConn = new P2PConnect(NetworkType.Tcp);
            ServerConn.Connect(Host, Port);
            // 开始循环处理tcp消息
            _ = ListenServerMessage(ServerConn);
            // 进行客户端登录
            ApiModel_Login_R? res = await Bus.Login(ServerConn.SendData, new ApiModel_Login() { LoginType = LoginType.Guest });
            if (res == null)
            {
                throw new Exception("服务器异常");
            }
            // 设置客户端唯一编码
            clientCode = res.ClientCode;
            LoginToken = res.LoginToken;

        }
        public void ConnectServer(string token)
        {
            throw new NotImplementedException();
        }
        public void ConnectServer(string userName, string psw)
        {
            throw new NotImplementedException();
        }

        public P2PConnect CreateP2PPipe(int localPort)
        {
            throw new NotImplementedException();
        }

        public void RemovePortMapItem(int localPort)
        {
            throw new NotImplementedException();
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
                StartListen(item.LocalPort, item.PortType);
            }
        }

        private void StartListen(int port, NetworkType type)
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
                                break;
                            }
                        case P2PMode.Tcp端口复用:
                        case P2PMode.Tcp端口预测:
                            {
                                break;
                            }
                        case P2PMode.Udp端口复用:
                            {
                                throw new NotSupportedException("暂不支持");
                            }
                        default:
                            throw new ArgumentException("未知的类型");
                    }
                }
                else
                {
                    conn.Close();
                }
            });
        }

        #region 服务器消息监听
        private async Task ListenServerMessage(P2PConnect conn)
        {
            CmdPacket packet = new CmdPacket(conn.Conn);
            try
            {
                while (true)
                {
                    byte[] buffer = await packet.ReadOne();
                    Bus.TaskUtil.Finish(packet.Token, buffer);
                    if (ExcuteMap.ContainsKey(packet.RequestType))
                    {
                        await ExcuteMap[packet.RequestType].Handle(this, conn, buffer, packet.Token);
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }
        #endregion

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

        private void Transfer_Server(INetworkConnect conn, PortMapItem item)
        {
            // 查询权限
        }
        #endregion

        #region P2P打洞
        private void Transfer_P2P(INetworkConnect conn, PortMapItem item)
        {

        }
        #endregion
    }
}
