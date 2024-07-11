using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PSocektLib
{
    internal class PipeConnect
    {
        /// <summary>
        /// 要连接的远端端口
        /// </summary>
        public int RemotePort { set; get; }
        /// <summary>
        /// 本地连接
        /// </summary>
        public INetworkConnect Connect { set;get; }
        public PipeConnect(INetworkConnect conn, int remotePort)
        {
            Connect = conn;
            RemotePort = remotePort;
        }

        public void Close()
        {
            try
            {
                Connect?.Close();
            }
            catch (Exception) { }
        }
    }
}
