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
        public int ServerPort { get; set; }

        [ConfigField("本地服务端口")]
        public int LocalServerPort { get; set; }

        [ConfigField("远程服务的本地端口")]
        public int RemoteLocalPort { get; set; }

        [ConfigField("服务器ip地址")]
        public string ServerIp { set; get; }

        [ConfigField("本地服务名称")]
        public string LocalServerName { set; get; } = "";

        [ConfigField("Http服务名称")]
        public string HttpServerName { set; get; }

        [ConfigField("日志等级")]
        public LogLevel LogLevel { set; get; } = LogLevel.Info;
    }
}
