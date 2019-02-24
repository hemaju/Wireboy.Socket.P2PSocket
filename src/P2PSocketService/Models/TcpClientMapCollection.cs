using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Linq;

namespace Wireboy.Socket.P2PService.Models
{
    public class TcpClientMapCollection<T> : List<T> where T : TcpClientMap 
    {
        public TcpClientMap this[TcpClient tcpClient]
        {
            get
            {
               return this.Where(t => t.IsMatch(tcpClient)).FirstOrDefault();
            }
        }

        public TcpClientMap this[string key]
        {
            get
            {
                return this.Where(t => t.Key == key).FirstOrDefault();
            }
        }

        public bool ContainsKey(string key)
        {
            return this[key] != null;
        }

        public bool ContainsClient(TcpClient tcpClient)
        {
            return this[tcpClient] != null;
        }
    }
}
