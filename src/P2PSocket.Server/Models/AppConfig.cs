using P2PSocket.Core.Enums;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using P2PSocket.Server.Models;
using P2PSocket.Server.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Server
{
    public class AppConfig : BaseConfig
    {
        public AppConfig()
        {
            Init();
        }
        private void Init()
        {
            LocalPort = 3488;
            P2PTimeout = 10000;
            P2PWaitConnectTime = 10000;
            PortMapList = new List<PortMapItem>();
            ClientAuthList = new List<ClientItem>();
            MacAddressMap = new Dictionary<string, string>();
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
            EasyInject.Get<IServerConfig>().SaveMacAddress(this);
            return name;
        }

        public LogLevel LogLevel { set; get; }
        /// <summary>
        ///     服务端口
        /// </summary>
        public int LocalPort { set; get; }
        /// <summary>
        ///     P2P内网穿透超时时间
        /// </summary>
        public int P2PTimeout { get; private set; }
        public int P2PWaitConnectTime { get; private set; }

        /// <summary>
        ///     本地端口映射（需要启动时初始化）
        /// </summary>
        public List<PortMapItem> PortMapList { set; get; }
        /// <summary>
        ///     客户端认证集合（为空时不验证AuthCode）
        /// </summary>
        public List<ClientItem> ClientAuthList { set; get; }

        /// <summary>
        ///     mac与客户端地址映射
        /// </summary>
        public Dictionary<string, string> MacAddressMap { set; get; }
    }
}
