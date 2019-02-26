/*
 * 
 *需要处理自己连接自己的情况 
 * 
 * 
 */
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Wireboy.Socket.P2PService.Models
{
    public class TcpClientMap
    {
        //public TcpClient[] TcpClients { set; get; }
        public TcpClient ToClient { set; get; }
        public TcpClient FromClient { set; get; }
        public string Key { set; get; }
        public TcpClientMap()
        {
            ToClient = null;
            FromClient = null;
        }

        public bool IsMatch(TcpClient tcpClient)
        {
            return ToClient == tcpClient || FromClient == tcpClient;
        }

    }
}
