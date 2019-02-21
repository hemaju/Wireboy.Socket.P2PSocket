using System;
using System.Collections.Generic;
using System.Text;

namespace Wireboy.Socket.P2PService.Models
{
    public static class ApplicationConfig
    {
        private const string _configFile = "serverconfig.ini";
        private static string _logFile = "P2PService.log";
        private static int _localPort = 3388;
        /// <summary>
        /// 日志文件名
        /// </summary>
        public static string LogFile { get => _logFile; set => _logFile = value; }
        /// <summary>
        /// 服务端口号
        /// </summary>
        public static int LocalPort { get => _localPort; set => _localPort = value; }
        /// <summary>
        /// 配置文件名
        /// </summary>
        public static string ConfigFile => _configFile;
    }
}
