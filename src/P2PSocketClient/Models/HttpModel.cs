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
        [ConfigField("本地网站端口号")]
        public int WebPort { set; get; }
        [ConfigField("类型（http/Other）")]
        public string Type { set; get; } = "http";
        [ConfigField("需要映射的ip地址,计算机名（如 127.0.0.1  ,myserver）")]
        public string LocIp { set; get; } = "127.0.0.1";
    }
}
