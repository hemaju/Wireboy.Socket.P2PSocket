using P2PSocektLib.Command;
using P2PSocektLib.Export;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
/*
 * 1.在接收到新的请求时
 * 
 * 
 */
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
        /// 本地连接
        /// </summary>
        public PipeConnect? LocalConn { set; get; }
        /// <summary>
        /// 管道是否已关闭
        /// </summary>
        public bool IsClosed { set; get; }
        /// <summary>
        /// 对比数字（用于双方同时请求使用时比较，大的一方拥有使用权）
        /// </summary>
        public int CmpareNum { set; get; }
        /// <summary>
        /// 数据发送实例
        /// </summary>
        Request_Pipe_Service bus = new Request_Pipe_Service();
        /// <summary>
        /// 在远端连接关闭时触发
        /// </summary>
        Action<P2PPipe>? OnPipeDestConnClosed { set; get; }



        /// <summary>
        /// 网络管道
        /// </summary>
        /// <param name="name">管道名称（目前传入的是客户端名称）</param>
        /// <param name="conn">网络连接（与服务端或者其它客户端的连接）</param>
        public P2PPipe(string name, P2PConnect conn)
        {
            IsClosed = true;
            CmpareNum = 0;
            Name = name;
            Conn = conn;
            _ = Open();
        }

        /// <summary>
        /// 开启管道
        /// </summary>
        /// <returns></returns>
        public async Task Open()
        {
            IsClosed = false;
            // 开始监听数据
            PipePacket packet = new PipePacket(Conn.Conn);
            try
            {
                while (true)
                {
                    byte[] buffer = await packet.ReadOne();
                    if (!packet.IsRequest)
                    {
                        bus.TaskUtil.Finish(packet.Token, buffer);
                    }
                    else
                    {
                        // 处理Request命令

                        switch (packet.RequestType)
                        {
                            case RequestEnum.管道_新建连接:
                                {
                                    if (LocalConn != null)
                                    {
                                        LocalConn.Close();
                                        LocalConn = null;
                                    }
                                    break;
                                }
                            case RequestEnum.管道_转发数据:
                                {
                                    if (LocalConn != null)
                                    {
                                        await LocalConn.Connect.SendData(buffer, buffer.Length);
                                    }
                                    break;
                                }
                            case RequestEnum.管道_断开连接:
                                {
                                    if (LocalConn != null)
                                    {
                                        LocalConn.Close();
                                        LocalConn = null;
                                    }
                                    break;
                                }
                            case RequestEnum.心跳:
                                {
                                    break;
                                }
                            default:
                                {
                                    // 异常数据
                                    break;
                                }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
            }
            IsClosed = true;
        }

        /// <summary>
        /// 关闭管道
        /// </summary>
        /// <returns></returns>
        public void Close()
        {
            // 关闭数据监听（关闭管道后，会自动断开本地连接，不用特别处理）
            Conn.Close();
        }

        /// <summary>
        /// 添加数据转发连接
        /// </summary>
        /// <param name="conn">外部连接（原始的tcp或者udp连接）</param>
        /// <param name="item"></param>
        public async Task<bool> TransferLocalConn(INetworkConnect conn, PortMapItem item)
        {
            PipeConnect pipeConnect = new PipeConnect(conn, item.RemotePort);
            try
            {
                // 申请连接
                ApiModel_Pipe_NotifyCreateConn_R? result = await bus.NotifyCreateConn(Conn.SendData, new ApiModel_Pipe_NotifyCreateConn(item.RemotePort));
                if (result != null && !result.IsSuccess)
                {
                    // 开始转发数据
                    _ = StartLocalTransfer(pipeConnect);
                    return true;
                }
            }
            catch (Exception)
            {
            }
            return false;
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
                        await bus.TransferData(Conn.SendData, buffer, length);
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
            try
            {
                // 发送连接断开消息
                await bus.NotifyCloseConn(Conn.SendData, new ApiModel_Pipe_NotifyCloseConn());
                OnPipeDestConnClosed?.Invoke(this);
            }
            catch (Exception) { }
        }
    }
}
