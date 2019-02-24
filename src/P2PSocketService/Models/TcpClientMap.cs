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
        public TcpClient[] TcpClients { set; get; }
        public string Key { set; get; }
        public TcpClientMap()
        {
            TcpClients = new TcpClient[2] { null, null }; 
        }

        public TcpClient this[TcpClient tcpClient]
        {
            set
            {
                if (tcpClient == null)
                    throw new ArgumentNullException();
                if (tcpClient == TcpClients[0])
                    TcpClients[1] = value;
                else
                    TcpClients[0] = value;
            }
            get
            {
                if (tcpClient != TcpClients[0])
                    if (tcpClient != TcpClients[0])
                        throw new NullReferenceException();
                    else
                        return TcpClients[0];
                else
                    return TcpClients[1];
            }
        }

        public bool IsMatch(TcpClient tcpClient)
        {
            return TcpClients[0] == tcpClient || TcpClients[1] == tcpClient;
        }

        public void Remove(TcpClient tcpClient)
        {

            if (tcpClient == TcpClients[0])
                TcpClients[0] = null;
            else if (tcpClient == TcpClients[1])
                TcpClients[1] = null;
        }
    }
}
