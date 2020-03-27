using P2PSocket.Core.Models;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace P2PSocket.Client
{
    public class TcpCenter
    {
        static TcpCenter m_instance = null;
        public static TcpCenter Instance
        {
            get
            {
                if(m_instance == null)
                {
                    m_instance = new TcpCenter();
                }
                return m_instance;
            }
        }
        private TcpCenter()
        {

        }
        /// <summary>
        ///     服务器Tcp连接
        /// </summary>
        internal P2PTcpClient P2PServerTcp { set; get; }
        public Dictionary<string, TcpListener> ListenerList { set; get; } = new Dictionary<string, TcpListener>();
        public List<P2PTcpClient> ConnectedTcpList { set; get; } = new List<P2PTcpClient>();
        /// <summary>
        ///     等待中的tcp连接
        /// </summary>
        public Dictionary<string, P2PTcpClient> WaiteConnetctTcp { get; } = new Dictionary<string, P2PTcpClient>();
    }
}
