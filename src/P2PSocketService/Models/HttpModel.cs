using System;
using System.Collections.Generic;
using System.Text;

namespace Wireboy.Socket.P2PService.Models
{
    public class HttpModel
    {
        [ConfigField("端口号")]
        public int Port { set; get; }
        [ConfigField("类型（http/Other）")]
        public string Type { set; get; } = "http";
        [ConfigField("域名（例如：blog.star110.com）")]
        public string Domain { set; get; }
        [ConfigField("站点服务名称")]
        public string ServerName { set; get; }
    }
}
/*

[httpServer]
{
    port = 80
    {
        type : http
        domain : star110.com
        ServerName : groupWeb
    }
    {
        type : http
        domain : novel.star110.com
        ServerName : novelWeb
    }
    {
        type : https
        ServerName : groupWeb
    }
}



*/
