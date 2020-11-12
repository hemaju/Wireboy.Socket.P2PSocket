using P2PSocket.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace P2PSocket.Client
{
    public class TcpCenter
    {
        public TcpCenter()
        {
            Init();
        }
        protected void Init()
        {
            ListenerList = new Dictionary<(string, int), TcpListener>();
            ConnectedTcpList = new List<P2PTcpClient>();
            WaiteConnetctTcp = new ConcurrentDictionary<string, P2PResult>();
        }
        /// <summary>
        ///     服务器Tcp连接
        /// </summary>
        internal P2PTcpClient P2PServerTcp { set; get; }
        public Dictionary<(string, int), TcpListener> ListenerList { set; get; }
        public List<P2PTcpClient> ConnectedTcpList { set; get; }
        /// <summary>
        ///     等待中的tcp连接
        /// </summary>
        public ConcurrentDictionary<string, P2PResult> WaiteConnetctTcp { get; private set; }
    }
}
