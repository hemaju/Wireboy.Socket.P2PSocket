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
        public TcpClient HomeClient { set; get; }
        public TcpClient ControlClient { set; get; }
        public string Key { set; get; }
        public TcpClientMap()
        {
            HomeClient = null;
            ControlClient = null;
        }

        public bool IsMatch(TcpClient tcpClient)
        {
            return HomeClient == tcpClient || ControlClient == tcpClient;
        }

    }
}
