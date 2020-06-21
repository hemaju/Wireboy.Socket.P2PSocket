using P2PSocket.Core.Models;
using P2PSocket.Server.Models;
using P2PSocket.Server.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Server
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

        }

        public string RegisterMacAddress(string mac)
        {
            Random random = new Random();
            string name = random.Next(303030, 909090).ToString();
            while (MacAddressMap.ContainsKey(name))
            {
                name = random.Next(303030, 909090).ToString();
            }
            MacAddressMap.Add(mac, name);
            ConfigUtils.SaveMacAddress(this);
            return name;
        }

        /// <summary>
        ///     服务端口
        /// </summary>
        public int LocalPort { set; get; } = 3488;
        /// <summary>
        ///     P2P内网穿透超时时间
        /// </summary>
        public int P2PTimeout { get; } = 10000;

        /// <summary>
        ///     本地端口映射（需要启动时初始化）
        /// </summary>
        public List<PortMapItem> PortMapList = new List<PortMapItem>();
        /// <summary>
        ///     客户端认证集合（为空时不验证AuthCode）
        /// </summary>
        public List<ClientItem> ClientAuthList { set; get; } = new List<ClientItem>();

        /// <summary>
        ///     mac与客户端地址映射
        /// </summary>
        public Dictionary<string, string> MacAddressMap = new Dictionary<string, string>();
    }
}
