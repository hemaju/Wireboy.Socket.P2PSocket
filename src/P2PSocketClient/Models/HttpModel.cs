using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wireboy.Socket.P2PClient.Models
{
    public class HttpModel
    {
        [ConfigField("域名（例如：blog.star110.com）")]
        public string Domain { set; get; }
        [ConfigField("ip地址")]
        public string WebIp { set; get; } = "127.0.0.1";
        [ConfigField("本地网站端口号")]
        public int WebPort { set; get; }
        [ConfigField("类型（http/Other）")]
        public string Type { set; get; } = "http";
    }
}
