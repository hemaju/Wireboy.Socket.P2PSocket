using P2PSocektLib.Command;
using P2PSocektLib.Export;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PSocektLib
{
    internal class P2PPipe
    {
        /// <summary>
        /// 管道token
        /// </summary>
        public string Name { set; get; }
        /// <summary>
        /// 管道连接
        /// </summary>
        public P2PConnect Conn { set; get; }
        /// <summary>
        /// 使用管道的网络连接
        /// </summary>
        public ConcurrentBag<INetworkConnect> networkConnects { set; get; }

        /// <summary>
        /// 网络管道
        /// </summary>
        /// <param name="name">管道名称（目前传入的是客户端名称）</param>
        /// <param name="conn">网络连接（与服务端或者其它客户端的连接）</param>
        public P2PPipe(string name, P2PConnect conn)
        {
            networkConnects = new ConcurrentBag<INetworkConnect>();
            Name = name;
            Conn = conn;
        }

        /// <summary>
        /// 开启管道
        /// </summary>
        /// <returns></returns>
        public async Task Open()
        {
            // 开始监听数据
        }

        /// <summary>
        /// 关闭管道
        /// </summary>
        /// <returns></returns>
        public async Task Close()
        {
            // 关闭连接
            // 关闭数据监听
        }

        /// <summary>
        /// 添加数据转发连接
        /// </summary>
        /// <param name="conn">外部连接（原始的tcp或者udp连接）</param>
        /// <param name="item"></param>
        public void AddConnect(INetworkConnect conn, PortMapItem item)
        {
            // 发送新的连接申请
            // 加入networkConnects
            // 开始转发数据
        }
    }
}
