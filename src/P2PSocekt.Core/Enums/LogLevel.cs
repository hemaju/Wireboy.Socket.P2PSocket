using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Core.Enums
{
    public enum LogLevel : int
    {
        /// <summary>
        ///     无日志模式
        /// </summary>
        None = 0,
        /// <summary>
        ///     致命消息
        /// </summary>
        Fatal = 1,
        /// <summary>
        ///     错误消息
        /// </summary>
        Error = 2,
        /// <summary>
        ///     警告消息
        /// </summary>
        Warning = 3,
        /// <summary>
        ///     一般消息
        /// </summary>
        Info = 4,
        /// <summary>
        ///     调试消息
        /// </summary>
        Debug = 5,
        /// <summary>
        ///     跟踪消息
        /// </summary>
        Trace = 6
    }
}
