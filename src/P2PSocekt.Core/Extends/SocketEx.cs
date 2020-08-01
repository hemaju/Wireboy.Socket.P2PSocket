using P2PSocket.Core.Utils;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace P2PSocket.Core.Extends
{
    public static class SocketEx
    {
        public static void SafeClose(this Socket socket)
        {
            if (socket.Connected)
            {
                socket.Close();
            }
        }

        public static void BeginSend(this TcpClient client, byte[] data)
        {
            client.Client.BeginSend(data, 0, data.Length, SocketFlags.None, sendCallback, client);
        }
        private static void sendCallback(IAsyncResult ar)
        {
            TcpClient tcp = (TcpClient)ar.AsyncState;
            EasyOp.Do(() => tcp.Client.EndSend(ar));
        }
    }
}
