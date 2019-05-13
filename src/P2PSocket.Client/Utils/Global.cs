using P2PSocket.Core;
using P2PSocket.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace P2PSocket.Client
{
    
    public static class Global
    {
        /// <summary>
        ///     配置文件路径
        /// </summary>
        public const string ConfigFile = @"P2PSocket/Client.ini";
        /// <summary>
        ///     服务器Tcp连接
        /// </summary>
        public static P2PTcpClient P2PServerTcp { set; get; }
        /// <summary>
        ///     服务器地址
        /// </summary>
        public static string ServerAddress { set; get; }
        /// <summary>
        ///     服务器端口
        /// </summary>
        public static int ServerPort { set; get; } = 3488;
        /// <summary>
        ///     全局的线程工厂
        /// </summary>
        public static TaskFactory TaskFactory { set; get; } = new TaskFactory();
        /// <summary>
        ///     等待中的tcp连接
        /// </summary>
        public static Dictionary<string, P2PTcpClient> WaiteConnetctTcp = new Dictionary<string, P2PTcpClient>();
        /// <summary>
        ///     允许处理不经过身份验证的消息类型
        /// </summary>
        public static List<P2PCommandType> AllowAnonymous { get; } = new List<P2PCommandType>() { P2PCommandType.Heart0x0052, P2PCommandType.Login0x0101, P2PCommandType.P2P0x0211, P2PCommandType.P2P0x0201 };



        /// <summary>
        ///     所有命令集合（需要启动时初始化）
        /// </summary>
        public static Type[] CommandList { set; get; }
        /// <summary>
        ///     本地端口映射（需要启动时初始化）
        /// </summary>
        public static List<PortMapItem> PortMapList = new List<PortMapItem>();
        /// <summary>
        ///     客户端服务名
        /// </summary>
        public static string ClientName { set; get; }
        /// <summary>
        ///     允许外部连接的端口白名单
        /// </summary>
        public static List<int> AllowPort { get; } = new List<int>();
    }


}
