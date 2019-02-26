using System;
using System.Collections.Generic;
using System.Text;
using Wireboy.Socket.P2PService.Models;

namespace Wireboy.Socket.P2PService.Services
{
    public static class ConfigServer
    {
        /// <summary>
        /// 配置文件名
        /// </summary>
        public const string ConfigFile = "config.ini";
        /// <summary>
        /// 日志文件名
        /// </summary>
        public const string LogFile = "P2PService.log";
        /// <summary>
        /// 配置
        /// </summary>
        public static ApplicationConfig AppSettings = new ApplicationConfig();

        /// <summary>
        /// 加载配置文件
        /// </summary>
        /// <param name="file"></param>
        public static void LoadFromFile()
        {

        }
        /// <summary>
        /// 保存配置文件
        /// </summary>
        public static void SaveToFile()
        {

        }
    }
}
