using System;
using System.Collections.Generic;
using System.Text;

namespace Wireboy.Socket.P2PService.Models
{
    public class HttpModel
    {
        public string Type { set; get; } = "http";
        public string Domain { set; get; }
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
