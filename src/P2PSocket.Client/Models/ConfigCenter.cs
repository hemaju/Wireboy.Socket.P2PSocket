using P2PSocket.Core;
using P2PSocket.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace P2PSocket.Client
{

    public class ConfigCenter
    {
        static ConfigCenter m_instance = null;

        public static ConfigCenter Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new ConfigCenter();
                }
                return m_instance;
            }
        }

        internal static void LoadConfig(ConfigCenter config)
        {
            m_instance = config;
        }

        public ConfigCenter()
        {
            Init();
        }

        public void Init()
        {
            ServerAddress = "";
            PortMapList = new List<PortMapItem>();
            ClientName = "";
            AuthCode = "";
            AllowPortList.Clear();
            BlackClients.Clear();
        }
        /// <summary>
        ///     服务器地址
        /// </summary>
        public string ServerAddress { set; get; }
        /// <summary>
        ///     服务器端口
        /// </summary>
        public int ServerPort { set; get; }
        /// <summary>
        ///     P2P内网穿透超时时间
        /// </summary>
        public const int P2PTimeout = 10000;
        /// <summary>
        ///     本地端口映射（需要启动时初始化）
        /// </summary>
        public List<PortMapItem> PortMapList = new List<PortMapItem>();
        /// <summary>
        ///     客户端服务名
        /// </summary>
        public string ClientName { set; get; }
        /// <summary>
        ///     客户端授权码
        /// </summary>
        public string AuthCode { set; get; }
        /// <summary>
        ///     允许外部连接的端口
        /// </summary>
        public List<AllowPortItem> AllowPortList { get; } = new List<AllowPortItem>();
        /// <summary>
        ///     客户端黑名单
        /// </summary>
        public List<string> BlackClients { get; } = new List<string>();
    }


}
