using P2PSocket.Core;
using P2PSocket.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace P2PSocket.Server
{
    public static class Global
    {
        /// <summary>
        ///     软件版本
        /// </summary>
        public const string SoftVerSion = "2.0.0";
        /// <summary>
        ///     通讯协议版本
        /// </summary>
        public const string DataVerSion = "1.0.0";
        /// <summary>
        ///     配置文件路径
        /// </summary>
        public const string ConfigFile = @"P2PSocket/Server.ini";
        /// <summary>
        ///     服务端口
        /// </summary>
        public static int LocalPort { set; get; } = 3488;
        /// <summary>
        ///     客户端的tcp映射<服务名,tcp>
        /// </summary>
        public static Dictionary<string, P2PTcpClient> TcpMap = new Dictionary<string, P2PTcpClient>();
        /// <summary>
        ///     全局的线程工厂
        /// </summary>
        public static TaskFactory TaskFactory { set; get; } = new TaskFactory();
        /// <summary>
        ///     等待中的tcp连接
        /// </summary>
        public static Dictionary<string, P2PTcpClient> WaiteConnetctTcp = new Dictionary<string, P2PTcpClient>();
        public static List<P2PCommandType> AllowAnonymous { get; } = new List<P2PCommandType>() { P2PCommandType.Heart0x0052, P2PCommandType.Login0x0101, P2PCommandType.P2P0x0211, P2PCommandType.P2P0x0201 };
        /// <summary>
        ///     P2P内网穿透超时时间
        /// </summary>
        public const int P2PTimeout = 10000;


        /// <summary>
        ///     所有命令集合（需要启动时初始化）
        /// </summary>
        public static Dictionary<P2PCommandType, Type> CommandDict { set; get; } = new Dictionary<P2PCommandType, Type>();
        /// <summary>
        ///     本地端口映射（需要启动时初始化）
        /// </summary>
        public static List<PortMapItem> PortMapList = new List<PortMapItem>();
    }


}
