using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Core.Models
{
    /// <summary>
    ///     端口映射类型
    /// </summary>
    public enum PortMapType : int
    {
        /// <summary>
        ///     服务器转发模式
        /// </summary>
        servername = 0,
        /// <summary>
        ///     直连模式
        /// </summary>
        ip = 1
    }
    public class PortMapItem
    {
        /// <summary>
        ///     映射的服务名或ip
        /// </summary>
        public string RemoteAddress { set; get; }
        /// <summary>
        ///     远端端口号
        /// </summary>
        public int RemotePort { set; get; }
        /// <summary>
        ///     本地端口号
        /// </summary>
        public int LocalPort { set; get; }

        /// <summary>
        ///     本地监听ip
        /// </summary>
        public string LocalAddress { set; get; }
        public int P2PType { set; get; } = 0;
        /// <summary>
        ///     映射类型
        /// </summary>
        public PortMapType MapType { set; get; } = PortMapType.servername;
        public PortMapItem()
        {
        }
    }
}
