using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Linq;

namespace Wireboy.Socket.P2PService.Models
{
    public class TcpClientMapHelper
    {
        private List<TcpClientMap> _mapList = new List<TcpClientMap>();
        public TcpClient this[TcpClient tcpClient]
        {
            set
            {
                TcpClientMap retClientMap = _mapList.Where(t => t.IsMatch(tcpClient)).FirstOrDefault();
                if(retClientMap.FromClient == tcpClient)
                {
                    retClientMap.ToClient = value;
                }
                else if(retClientMap.ToClient == tcpClient)
                {
                    retClientMap.FromClient = value;
                }
            }
            get
            {
               TcpClientMap retClientMap = _mapList.Where(t => t.IsMatch(tcpClient)).FirstOrDefault();
                if(retClientMap == null)
                {
                    return null;
                }
                else if(retClientMap.FromClient == tcpClient)
                {
                    return retClientMap.ToClient;
                }
                else if(retClientMap.ToClient == tcpClient)
                {
                    return retClientMap.FromClient;
                }
                else
                {
                    return null;
                }
            }
        }

        public TcpClientMap this[String key]
        {
            get
            {
                TcpClientMap map = _mapList.Where(t => t.Key == key).FirstOrDefault();
                if (map == null)
                    map = new TcpClientMap() { Key = key };
                _mapList.Add(map);
                return map;
            }
        }

        public bool ContainsToClient(TcpClient toClient)
        {
            return _mapList.Where(t => t.ToClient == toClient).FirstOrDefault() != null;
        }

        public bool ContainsFromClient(TcpClient fromClient)
        {
            return _mapList.Where(t => t.ToClient == fromClient).Count() > 0;
        }

        public void SetFromClient(TcpClient fromClient, string key)
        {
            if(ContainsFromClient(fromClient))
            {
                _mapList.Where(t => t.FromClient == fromClient).FirstOrDefault().FromClient = null;
            }
            this[key].FromClient = fromClient;
        }

        public void SetToClient(TcpClient toClient, string key)
        {
            if (ContainsFromClient(toClient))
            {
                _mapList.Where(t => t.ToClient == toClient).FirstOrDefault().ToClient = null;
            }
            this[key].ToClient = toClient;
        }

        public TcpClient GetToClient(TcpClient fromClient)
        {
            TcpClientMap clientMap = _mapList.Where(t => t.FromClient == fromClient).FirstOrDefault();
            if (clientMap != null)
            {
                return clientMap.ToClient;
            }
            else
                return null;
        }

        public TcpClient GetFromClient(TcpClient toClient)
        {
            TcpClientMap clientMap = _mapList.Where(t => t.ToClient == toClient).FirstOrDefault();
            if (clientMap != null)
            {
                return clientMap.FromClient;
            }
            else
                return null;
        }
    }
}
