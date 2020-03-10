using P2PSocket.Core.Models;
using P2PSocket.Server.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Server
{
    public class ClientCenter
    {
        static ClientCenter m_instance = null;
        public static ClientCenter Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new ClientCenter();
                }
                return m_instance;
            }
        }
        private ClientCenter()
        {

        }
        /// <summary>
        ///     等待中的tcp连接
        /// </summary>
        public Dictionary<string, P2PTcpClient> WaiteConnetctTcp = new Dictionary<string, P2PTcpClient>();
        /// <summary>
        ///     客户端的tcp映射<服务名,tcp>
        /// </summary>
        public Dictionary<string, P2PTcpItem> TcpMap = new Dictionary<string, P2PTcpItem>();
    }
}
