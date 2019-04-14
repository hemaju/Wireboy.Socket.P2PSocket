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
        /// <summary>
        /// 获取映射的tcp
        /// </summary>
        /// <param name="tcpClient">当前tcp</param>
        /// <param name="IsControl">当前tcp是否主控端</param>
        /// <returns></returns>
        public TcpClient this[TcpClient tcpClient,bool IsControl]
        {
            set
            {
                TcpClientMap retClientMap = _mapList.Where(t => IsControl?t.ControlClient ==tcpClient:t.HomeClient == tcpClient).FirstOrDefault();
                if(IsControl)
                {
                    retClientMap.HomeClient = value;
                }
                else
                {
                    retClientMap.ControlClient = value;
                }
            }
            get
            {
               TcpClientMap retClientMap = _mapList.Where(t => IsControl ? t.ControlClient == tcpClient : t.HomeClient == tcpClient).FirstOrDefault();
                if (retClientMap == null)
                {
                    return null;
                }
                else if (IsControl)
                {
                    return retClientMap.HomeClient;
                }
                else
                {
                    return retClientMap.ControlClient;
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

        public bool ContainsControlClient(TcpClient fromClient)
        {
            return _mapList.Where(t => t.ControlClient == fromClient).FirstOrDefault() != null;
        }

        public bool ContainsHomeClient(TcpClient fromClient)
        {
            return _mapList.Where(t => t.HomeClient == fromClient).Count() > 0;
        }

        public void SetControlClient(TcpClient controlClient, string key)
        {
            if(ContainsControlClient(controlClient))
            {
                _mapList.Where(t => t.ControlClient == controlClient).FirstOrDefault().ControlClient = null;
            }
            this[key].ControlClient = controlClient;
        }

        public void SetLocalServerClinet(TcpClient homeClient, string key)
        {
            if (ContainsHomeClient(homeClient))
            {
                _mapList.Where(t => t.HomeClient == homeClient).FirstOrDefault().HomeClient = null;
            }
            this[key].HomeClient = homeClient;
        }
    }
}
