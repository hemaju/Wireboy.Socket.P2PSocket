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
        /// 本地连接位移标识
        /// </summary>
        public int LocalId { set; get; }
        /// <summary>
        /// 远端连接唯一标识
        /// </summary>
        public int RemoteId { set; get; }
        /// <summary>
        /// 要连接的远端端口
        /// </summary>
        public int RemotePort { set; get; }
        /// <summary>
        /// 本地连接
        /// </summary>
        public INetworkConnect Connect { set;get; }
        public PipeConnect(int id, INetworkConnect conn, int remotePort)
        {
            LocalId = id;
            Connect = conn;
            RemoteId = -1;
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
