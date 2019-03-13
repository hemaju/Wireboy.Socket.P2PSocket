using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wireboy.Socket.P2PClient.Models
{
    public class ApplicationConfig
    {
        [ConfigField("服务器通讯端口")]
        public int ServerPort { get; set; } = 3488;

        [ConfigField("服务器ip地址")]
        public string ServerIp { set; get; } = "127.0.0.1";

        [ConfigField("本地Home服务端口")]
        public int LocalHomePort { get; set; } = 3389;

        [ConfigField("本地Client服务端口")]
        public int LocalClientPort { get; set; } = 3588;

        [ConfigField("本地Home服务名称")]
        public string HomeServerName { set; get; } = "";
    }
}
