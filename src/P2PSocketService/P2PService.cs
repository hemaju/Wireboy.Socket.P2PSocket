/*
 * 主服务
 * 接收心跳包、客户端与服务器的数据交流
 * 
 */
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;

namespace Wireboy.Socket.P2PService
{
    public class P2PService
    {
        public Dictionary<string, TcpClient> socketDic = new Dictionary<string, TcpClient>();
        public P2PService()
        {

        }

        public void Start()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 3388);//这里开对方可以被你连接并且未被占用的端口  
            listener.Start();
            while (true)
            {
                TcpClient socket = listener.AcceptTcpClient();
                Logger.Write("接收到tcp请求:{0}", socket.Client.RemoteEndPoint);
                try
                {
                    clientConnect(socket, listener);
                }
                catch (Exception ex)
                {

                }
            }
        }

        private void clientConnect(TcpClient client, TcpListener listener)
        {
            TaskFactory taskFactory = new TaskFactory();
            taskFactory.StartNew(() => { clientReceive(client); });
        }

        Dictionary<TcpClient, string> transferClientDic = new Dictionary<TcpClient, string>();
        private void clientReceive(TcpClient client)
        {
            while (true)
            {
                byte[] recBytes = new byte[10240];
                int count = 0;
                try
                {
                    count = client.Client.Receive(recBytes);
                    Console.WriteLine("接收到数据：{0} 长度：{1}", client.Client.RemoteEndPoint, count);
                }
                catch (Exception ex)
                {
                    break;
                }
                if (count > 0)
                {
                    if (transferClientDic.ContainsKey(client))
                    {
                        //转发数据
                        if (socketDic.ContainsKey(transferClientDic[client]))
                        {
                            try
                            {
                                Console.WriteLine("转发数据到：{0} 长度：{1}", socketDic[transferClientDic[client]].Client.RemoteEndPoint, count);
                                NetworkStream ss = socketDic[transferClientDic[client]].GetStream();// Client.Send(recBytes);
                                ss.WriteAsync(recBytes, 0, count).ContinueWith(t=>recBytes = null);
                            }
                            catch (Exception ex)
                            {
                                socketDic[transferClientDic[client]] = null;
                            }
                        }
                    }
                    else if (socketDic.ContainsValue(client))
                    {
                        string str = socketDic.Where(t => t.Value == client).FirstOrDefault().Key;
                        if (transferClientDic.ContainsValue(str))
                        {
                            TcpClient tcpClient = transferClientDic.Where(t => t.Value == str).FirstOrDefault().Key;
                            try
                            {
                                Console.WriteLine("转发数据到：{0} 长度：{1}", tcpClient.Client.RemoteEndPoint, count);
                                NetworkStream ss = tcpClient.GetStream();
                                ss.WriteAsync(recBytes, 0, count).ContinueWith(t => recBytes = null);
                            }
                            catch (Exception ex)
                            {
                                transferClientDic.Remove(tcpClient);
                            }
                        }
                    }
                    else
                    {
                        //设置控制还是被控制
                        if (recBytes[0] == 55)
                        {
                            string str = Encoding.ASCII.GetString(recBytes, 1, count - 1).Trim();
                            //被控制
                            if (socketDic.ContainsKey(str))
                            {
                                socketDic[str] = client;
                            }
                            else
                            {
                                socketDic.Add(str, client);
                            }
                        }
                        else if (recBytes[0] == 56)
                        {
                            string str = Encoding.ASCII.GetString(recBytes, 1, count - 1).Trim();
                            if (transferClientDic.ContainsValue(str))
                            {
                                TcpClient key = transferClientDic.Where(t => t.Value == str).FirstOrDefault().Key;
                                transferClientDic.Remove(key);
                            }
                            //控制
                            if (transferClientDic.ContainsKey(client))
                            {
                                Console.WriteLine("插入key:{0} value:{1}", client.Client.RemoteEndPoint, str);
                                transferClientDic[client] = str;
                            }
                            else
                            {
                                Console.WriteLine("更新key:{0} value:{1}", client.Client.RemoteEndPoint, str);
                                transferClientDic.Add(client, str);
                            }
                        }
                        recBytes = null;
                    }
                }
                else
                {
                    recBytes = null;
                    Thread.Sleep(50);
                }
            }
        }
    }
}
