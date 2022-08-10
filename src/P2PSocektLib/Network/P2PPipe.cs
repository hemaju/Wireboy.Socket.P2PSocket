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
        public ConcurrentBag<PipeConnect> networkConnects { set; get; }
        /// <summary>
        /// 命名管道唯一标识
        /// </summary>
        public string? Token { set; get; }

        private int connId { set; get; }
        /// <summary>
        /// 网络管道
        /// </summary>
        /// <param name="name">管道名称（目前传入的是客户端名称）</param>
        /// <param name="conn">网络连接（与服务端或者其它客户端的连接）</param>
        public P2PPipe(string name, P2PConnect conn)
        {
            networkConnects = new ConcurrentBag<PipeConnect>();
            Name = name;
            Conn = conn;
            connId = 0;
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
            connId = (connId + 1) % int.MaxValue;
            int curId = connId;
            // 加入networkConnects
            PipeConnect pipeConnect = new PipeConnect(curId, conn, item.RemotePort);
            networkConnects.Add(pipeConnect);
            // 开始转发数据
            _ = StartLocalTransfer(pipeConnect);
        }

        /// <summary>
        /// 开始转发本地连接数据到远端
        /// </summary>
        /// <param name="st">本地连接实例</param>
        /// <returns></returns>
        private async Task StartLocalTransfer(PipeConnect st)
        {
            // 发送开始消息
            byte[] buffer = new byte[1024];
            int length;
            do
            {
                try
                {
                    length = await st.Connect.ReadData(buffer, 1024);
                }
                catch
                {
                    // 发送断开控制消息
                    Control_ConnClosed(st);
                    break;
                }
                if (length != 0)
                {
                    Console.WriteLine(Encoding.UTF8.GetString(buffer, 0, length));
                    try
                    {
                        await SendData(buffer, length);
                    }
                    catch (Exception ex)
                    {
                        // 关闭本地连接
                        st.Close();
                        break;
                    }
                }
                else
                {
                    // 发送断开控制消息
                    Control_ConnClosed(st);
                }
            } while (length != 0);
        }

        /// <summary>
        /// 发送连接控制消息（准备连接，断开）
        /// </summary>
        /// <param name="st"></param>
        /// <exception cref="NotImplementedException"></exception>
        private async void Control_ConnClosed(PipeConnect st)
        {
            // 发送连接断开消息


            await Conn.SendData();
            throw new NotImplementedException();
        }

        /// <summary>
        /// 向远端发送数据
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private async Task SendData(byte[] buffer, int length)
        {
            //[命令][id][port][数据]
        }
    }
}
