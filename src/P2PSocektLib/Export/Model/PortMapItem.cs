using P2PSocektLib.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PSocektLib.Export
{
    public class PortMapItem
    {

        /// <summary>
        /// 本地端口类型（tcp/udp)
        /// </summary>
        public NetworkType PortType { set; get; }
        /// <summary>
        /// 本地端口
        /// </summary>
        public int LocalPort { set; get; }
        /// <summary>
        /// 映射的目标地址
        /// </summary>
        public string RemoteAddress { set; get; }
        /// <summary>
        /// 映射的目标端口
        /// </summary>
        public int RemotePort { set; get; }
        /// <summary>
        /// 连接类型（服务器中转、P2P端口复用...）
        /// </summary>
        public P2PMode ConnectType { set; get; }
        /// <summary>
        /// 是否单通道模式
        /// </summary>
        public bool IsSingle { set; get; }
        public PortMapItem(int localPort, int remotePort, string remoteAddress, P2PMode connectType = P2PMode.服务器中转, bool isSignle = true, NetworkType portType = NetworkType.Tcp)
        {
            this.LocalPort = localPort;
            this.RemoteAddress = remoteAddress;
            this.RemotePort = remotePort;
            this.ConnectType = connectType;
            this.IsSingle = isSignle;
            this.PortType = portType;
        }
    }
}
