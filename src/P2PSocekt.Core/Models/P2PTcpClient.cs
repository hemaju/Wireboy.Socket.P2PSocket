using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace P2PSocket.Core.Models
{
    public class P2PTcpClient : TcpClient
    {
        public P2PTcpClient() : base() { }
        public P2PTcpClient(AddressFamily family) : base(family) { }
        public P2PTcpClient(IPEndPoint localEP) : base(localEP) { }
        public P2PTcpClient(string hostname, int port) : base(hostname, port)
        {
            m_remoteEndPoint = Client.RemoteEndPoint.ToString();
            m_localEndPoint = Client.LocalEndPoint.ToString();
        }
        public P2PTcpClient(TcpClient tcpClient) : base()
        {
            ReceiveTimeout = tcpClient.ReceiveTimeout;
            ReceiveBufferSize = tcpClient.ReceiveBufferSize;
            NoDelay = tcpClient.NoDelay;
            LingerState = tcpClient.LingerState;
            ExclusiveAddressUse = tcpClient.ExclusiveAddressUse;
            Client = tcpClient.Client;
            SendBufferSize = tcpClient.SendBufferSize;
            SendTimeout = tcpClient.SendTimeout;
            Active = true;
        }

        public P2PTcpClient(Socket socket)
        {
            Client = socket;
            Active = Client.Connected;
            m_remoteEndPoint = socket.RemoteEndPoint.ToString();
            m_localEndPoint = socket.LocalEndPoint.ToString();
        }


        public string Token { set; get; } = Guid.NewGuid().ToString();
        public P2PTcpClient ToClient { set; get; }
        public bool IsAuth { set; get; } = false;

        String m_remoteEndPoint = "";
        String m_localEndPoint = "";
        public String RemoteEndPoint
        {
            get
            {
                return m_remoteEndPoint;
            }
        }
        public String LocalEndPoint
        {
            get
            {
                return m_localEndPoint;
            }
        }
    }
}
