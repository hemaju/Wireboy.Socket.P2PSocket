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
                try
                {
                    socket.Close();
                }
                finally
                {

                }
            }
        }
    }
}
