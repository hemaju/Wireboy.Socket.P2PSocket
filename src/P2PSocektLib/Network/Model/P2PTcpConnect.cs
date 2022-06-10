using P2PSocektLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace P2PSocektLib
{
    public class P2PTcpConnect : INetworkConnect
    {
        private TcpClient pClient;
        NetworkStream? stream;
        NetworkStream Stream
        {
            get
            {
                if (stream == null)
                {
                    stream = pClient.GetStream();
                }
                return stream;
            }
        }

        public P2PTcpConnect(TcpClient pClient)
        {
            this.pClient = pClient;
        }

        public void Close()
        {
            pClient.Close();
        }

        public void Connect(string ip, int port)
        {
            pClient.Connect(ip, port);
        }

        public IPAddress GetLocalAddress()
        {
            throw new NotImplementedException();
        }

        public IPAddress GetRemoteAddress()
        {
            throw new NotImplementedException();
        }

        public async Task SendData(byte[] data, int length)
        {
            await Stream.WriteAsync(data, 0, length);
        }

        public async Task<int> ReadData(byte[] data, int length)
        {
            return await Stream.ReadAsync(data, 0, length);
        }
    }
}
