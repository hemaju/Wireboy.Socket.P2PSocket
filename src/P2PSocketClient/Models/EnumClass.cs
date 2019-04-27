using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wireboy.Socket.P2PClient.Models
{
    /*
     * [86][86][类型1][类型2][长度][数据]
     * [2字节][1字节][1字节][2字节][...]
     *  
     */

    /// <summary>
    /// 协议类别
    /// </summary>
    public static class P2PSocketType
    {
        /// <summary>
        /// 心跳包
        /// </summary>
        public static class Heart
        {
            /// <summary>
            /// 字节码
            /// </summary>
            public const byte Code = 11;
        }
        /// <summary>
        /// 远程服务
        /// </summary>
        public static class Remote
        {
            /// <summary>
            /// 字节码
            /// </summary>
            public const byte Code = 12;
            /// <summary>
            /// 数据转发
            /// </summary>
            public static class Transfer { public const byte Code = 01; }
            /// <summary>
            /// 连接断开
            /// </summary>
            public static class Break { public const byte Code = 02; }
            /// <summary>
            /// 安全认证
            /// </summary>
            public static class Secure { public const byte Code = 03; }
            /// <summary>
            /// 服务名称
            /// </summary>
            public static class ServerName { public const byte Code = 04; }
            /// <summary>
            /// 错误消息
            /// </summary>
            public static class Error { public const byte Code = 09; }
        }
        /// <summary>
        /// 本地服务
        /// </summary>
        public static class Local
        {
            /// <summary>
            /// 字节码
            /// </summary>
            public const byte Code = 13;
            /// <summary>
            /// 数据转发
            /// </summary>
            public static class Transfer { public const byte Code = 01; }
            /// <summary>
            /// 连接断开
            /// </summary>
            public static class Break { public const byte Code = 02; }
            /// <summary>
            /// 安全认证
            /// </summary>
            public static class Secure { public const byte Code = 03; }
            /// <summary>
            /// 服务名称
            /// </summary>
            public static class ServerName { public const byte Code = 04; }
            /// <summary>
            /// 错误消息
            /// </summary>
            public static class Error { public const byte Code = 09; }
        }
        /// <summary>
        /// Http服务
        /// </summary>
        public static class Http
        {
            /// <summary>
            /// 字节码
            /// </summary>
            public const byte Code = 14;
            /// <summary>
            /// 数据转发
            /// </summary>
            public static class Transfer { public const byte Code = 01; }
            /// <summary>
            /// 连接断开
            /// </summary>
            public static class Break { public const byte Code = 02; }
            /// <summary>
            /// 安全认证
            /// </summary>
            public static class Secure { public const byte Code = 03; }
            /// <summary>
            /// 服务名称
            /// </summary>
            public static class ServerName { public const byte Code = 04; }
            /// <summary>
            /// 错误消息
            /// </summary>
            public static class Error { public const byte Code = 09; }
        }
        /// <summary>
        /// 认证服务
        /// </summary>
        public static class Secure
        {
            /// <summary>
            /// 字节码
            /// </summary>
            public const byte Code = 20;
            /// <summary>
            /// 安全认证
            /// </summary>
            public static class Confirm { public const byte Code = 01; }
            /// <summary>
            /// 错误消息
            /// </summary>
            public static class Error { public const byte Code = 09; }
        }
    }
    public enum LogLevel
    {
        /// <summary>
        /// 不记录日志
        /// </summary>
        None = 0,
        /// <summary>
        /// 错误消息
        /// </summary>
        Error = 1,
        /// <summary>
        /// 一般消息
        /// </summary>
        Info = 2,
        /// <summary>
        /// 调试消息
        /// </summary>
        Debug = 3,
        /// <summary>
        /// 跟踪数据
        /// </summary>
        Trace = 4
    }
}
