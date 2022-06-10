using P2PSocektLib.Enum;
using P2PSocektLib.Network.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace P2PSocektLib
{
    public class P2PListener
    {
        INetworkListener Listener;
        public P2PListener(int port, NetworkType type = NetworkType.Tcp)
        {
            switch (type)
            {
                case NetworkType.Tcp:
                    {
                        Listener = new P2PTcpListener();
                        Listener.Bind(port);
                        break;
                    }
                default: throw new ArgumentException();
            }
        }

        public void BindAcceptConnectionEvent(AcceptConnectionEventCallback action)
        {
            Listener.BindAcceptConnectionEvent(action);
        }

        public void Start()
        {
            Listener.Start();
        }

        public void Stop()
        {
            Listener.Stop();
        }
    }
}
