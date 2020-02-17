using P2PSocket.Core;
using P2PSocket.Core.Models;
using P2PSocket.Server.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace P2PSocket.Server
{
    public static class Global
    {
        /// <summary>
        ///     软件版本
        /// </summary>
        public const string SoftVerSion = "3.0.0";
        /// <summary>
        ///     通讯协议版本
        /// </summary>
        /// <summary>
        ///     运行目录
        /// </summary>
        public static string RuntimePath { get { return AppDomain.CurrentDomain.BaseDirectory; } }
        /// <summary>
        ///     配置文件路径
        /// </summary>
        public static string ConfigFile { get { return Path.Combine(RuntimePath, "P2PSocket", "Server.ini"); } }

        /// <summary>
        ///     服务端口
        /// </summary>
        public static int LocalPort { set; get; } = 3488;
        /// <summary>
        ///     客户端的tcp映射<服务名,tcp>
        /// </summary>
        public static Dictionary<string, P2PTcpItem> TcpMap = new Dictionary<string, P2PTcpItem>();
        /// <summary>
        ///     全局的线程工厂
        /// </summary>
        public static TaskFactory TaskFactory { set; get; } = new TaskFactory();
        /// <summary>
        ///     等待中的tcp连接
        /// </summary>
        public static Dictionary<string, P2PTcpClient> WaiteConnetctTcp = new Dictionary<string, P2PTcpClient>();
        public static List<P2PCommandType> AllowAnonymous { get; } = new List<P2PCommandType>() { P2PCommandType.Heart0x0052
            , P2PCommandType.Login0x0101
            , P2PCommandType.Login0x0103
            , P2PCommandType.P2P0x0211
            , P2PCommandType.P2P0x0201
            , P2PCommandType.Msg0x0301 };
        /// <summary>
        ///     P2P内网穿透超时时间
        /// </summary>
        public const int P2PTimeout = 10000;
        /// <summary>
        ///     当前主服务Guid
        /// </summary>
        public static Guid CurrentGuid { set; get; } = Guid.NewGuid();


        /// <summary>
        ///     所有命令集合（需要启动时初始化）
        /// </summary>
        public static Dictionary<P2PCommandType, Type> CommandDict { set; get; } = new Dictionary<P2PCommandType, Type>();
        /// <summary>
        ///     本地端口映射（需要启动时初始化）
        /// </summary>
        public static List<PortMapItem> PortMapList = new List<PortMapItem>();
        /// <summary>
        ///     客户端认证集合（为空时不验证AuthCode）
        /// </summary>
        public static List<ClientItem> ClientAuthList = new List<ClientItem>();
    }


}
