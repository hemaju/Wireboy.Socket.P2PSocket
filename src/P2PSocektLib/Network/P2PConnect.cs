using P2PSocektLib.Enum;
using P2PSocektLib.Network.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace P2PSocektLib
{
    public class P2PConnect
    {
        public INetworkConnect Conn;
        public P2PConnect(NetworkType type = NetworkType.Tcp)
        {
            switch (type)
            {
                case NetworkType.Tcp: Conn = new P2PTcpConnect(new System.Net.Sockets.TcpClient()); break;
                default: throw new ArgumentException();
            }
        }
        public P2PConnect(INetworkConnect conn)
        {
            Conn = conn;
        }
        public void Close()
        {
            Conn.Close();
        }

        public void Connect(string ip, int port)
        {
            Conn.Connect(ip, port);
        }

        public IPAddress GetLocalAddress()
        {
            throw new NotImplementedException();
        }

        public IPAddress GetRemoteAddress()
        {
            throw new NotImplementedException();
        }

        public async Task<int> ReadData(byte[] data, int length)
        {
            return await Conn.ReadData(data, length);
        }

        public async Task SendData(byte[] data, int length = -1)
        {
            if (length == -1) length = data.Length;
            await Conn.SendData(data, length);
        }
    }
}
