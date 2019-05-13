using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace P2PSocket.Core.Extends
{
    public static class NetworkStreamEx
    {
        public static int ReadSafe(this NetworkStream stream, byte[] buffer,int offset,int size)
        {
            try
            {
                return stream.Read(buffer, offset, size);
            }
            catch {
                return 0;
            }
        }
    }
}
