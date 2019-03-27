using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wireboy.Socket.P2PClient.Models
{
    public class ApplicationConfig
    {
        [ConfigField("服务器端口")]
        public int ServerPort { get; set; } = 3488;

        [ConfigField("服务器ip地址")]
        public string ServerIp { set; get; } = "127.0.0.1";

        [ConfigField("本地Home服务名称")]
        public string HomeServerName { set; get; } = "";

        [ConfigField("本地Home服务端口")]
        public int LocalHomePort { get; set; } = 3389;

        [ConfigField("本地Client服务端口")]
        public int LocalClientPort { get; set; } = 3388;

        [ConfigField("本地Http服务名称")]
        public string HttpServerName { set; get; }

        [ConfigField("日志等级")]
        public LogLevel LogLevel { set; get; } = LogLevel.运行模式;
    }
}
