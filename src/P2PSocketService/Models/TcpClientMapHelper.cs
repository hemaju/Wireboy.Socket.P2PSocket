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
                if(retClientMap.ControlClient == tcpClient)
                {
                    retClientMap.HomeClient = value;
                }
                else if(retClientMap.HomeClient == tcpClient)
                {
                    retClientMap.ControlClient = value;
                }
            }
            get
            {
               TcpClientMap retClientMap = _mapList.Where(t => t.IsMatch(tcpClient)).FirstOrDefault();
                if(retClientMap == null)
                {
                    return null;
                }
                else if(retClientMap.ControlClient == tcpClient)
                {
                    return retClientMap.HomeClient;
                }
                else if(retClientMap.HomeClient == tcpClient)
                {
                    return retClientMap.ControlClient;
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

        public bool ContainsControlClient(TcpClient homeClient)
        {
            return _mapList.Where(t => t.HomeClient == homeClient).FirstOrDefault() != null;
        }

        public bool ContainsHomeClient(TcpClient fromClient)
        {
            return _mapList.Where(t => t.HomeClient == fromClient).Count() > 0;
        }

        public void SetControlClient(TcpClient controlClient, string key)
        {
            if(ContainsHomeClient(controlClient))
            {
                _mapList.Where(t => t.ControlClient == controlClient).FirstOrDefault().ControlClient = null;
            }
            this[key].ControlClient = controlClient;
        }

        public void SetHomeClient(TcpClient homeClient, string key)
        {
            if (ContainsHomeClient(homeClient))
            {
                _mapList.Where(t => t.HomeClient == homeClient).FirstOrDefault().HomeClient = null;
            }
            this[key].HomeClient = homeClient;
        }

        public TcpClient GetHomeClient(TcpClient ControlClient)
        {
            TcpClientMap clientMap = _mapList.Where(t => t.ControlClient == ControlClient).FirstOrDefault();
            if (clientMap != null)
            {
                return clientMap.HomeClient;
            }
            else
                return null;
        }

        public TcpClient GetControlClient(TcpClient homeClient)
        {
            TcpClientMap clientMap = _mapList.Where(t => t.HomeClient == homeClient).FirstOrDefault();
            if (clientMap != null)
            {
                return clientMap.ControlClient;
            }
            else
                return null;
        }
    }
}
