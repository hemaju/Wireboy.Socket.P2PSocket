using P2PSocket.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Server.Models
{
    public class P2PTcpItem
    {
        public P2PTcpClient TcpClient { get; set; }
        public List<AllowPortItem> AllowPorts { get; set; } = new List<AllowPortItem>();
        public List<string> BlackClients { set; get; } = new List<string>();
    }
}
