using System;
using System.Collections.Generic;
using System.Text;

namespace Wireboy.Socket.P2PService.Models
{
    public static class ApplicationConfig
    {
        /// <summary>
        /// 配置文件名
        /// </summary>
        public static string ConfigFile { get; } = "serverconfig.ini";
        /// <summary>
        /// 日志文件名
        /// </summary>
        public static string LogFile { get; set; } = "P2PService.log";
        /// <summary>
        /// 服务器通讯端口号
        /// </summary>
        public static int ServerPort { get; set; } = 3388;
        /// <summary>
        /// 数据转发端口号
        /// </summary>
        public static int TransferPort { get; set; } = 3388;
    }
}
