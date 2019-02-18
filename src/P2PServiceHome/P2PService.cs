using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;

namespace P2PServiceHome
{
    public class P2PService
    {
        string service_IpAddress = "39.105.116.163";
        int service_Port = 3388;
        string ServerName = "MyPC";
        List<Task> taskList = new List<Task>();

        TcpClient outClient = null;
        TcpClient inClient = null;
        DateTime lastReceiveTime = DateTime.Now;

        TcpClient heartClient = null;
        Guid inGuid = Guid.NewGuid();
        Guid outGuid = Guid.NewGuid();
        TaskFactory taskFactory = new TaskFactory();
        public P2PService()
        {
        }
        public void Start()
        {
            lastReceiveTime = DateTime.Now;
            do
            {
                Console.WriteLine("请输入当前服务名称");
                ServerName = Console.ReadLine();
            } while (string.IsNullOrEmpty(ServerName));
            Console.WriteLine(string.Format("当前服务名称：{0}", ServerName));
            SendHeart();
        }



        public void SendHeart()
        {
            NetworkStream ss = null;
            do
            {
                try
                {
                    if (heartClient == null)
                    {
                        heartClient = new TcpClient(service_IpAddress, service_Port);
                        ss = heartClient.GetStream();
                    }
                    else
                    {
                        ss.WriteAsync(new byte[] { 0 }, 0, 1);
                    }
                }
                catch (Exception ex)
                {
                    heartClient = null;
                    outClient = null;
                    //Console.WriteLine("服务器连接失败，稍后将重连... {0}", ex);
                }
                try
                {
                    if (outClient == null || !outClient.Connected)
                    {
                        //Console.WriteLine("正在连接服务器... {0}:{1}", service_IpAddress, service_Port);
                        outClient = new TcpClient(service_IpAddress, service_Port);
                        //Console.WriteLine("服务器成功连接");

                        NetworkStream ssOut = outClient.GetStream();
                        List<byte> sMsg = Encoding.ASCII.GetBytes(ServerName).ToList();
                        sMsg.Insert(0, 55);
                        ssOut.Write(sMsg.ToArray(), 0, sMsg.ToArray().Length);
                        Guid guid = Guid.NewGuid();
                        outGuid = guid;
                        taskFactory.StartNew(() => { clientReceive(outClient, guid); });
                    }
                }
                catch (Exception ex)
                {
                    outClient = null;
                    //Console.WriteLine("服务器连接失败，稍后将重连... {0}", ex);
                }
                try
                {
                    if (inClient == null || !inClient.Connected)
                    {
                        //Console.WriteLine("正在连接本地远程桌面服务... 127.0.0.1:3389");
                        inClient = new TcpClient();
                        inClient.Connect(IPAddress.Parse("127.0.0.1"), 3389);
                        //Console.WriteLine("本地远程桌面服务连接成功");
                        Guid guid = Guid.NewGuid();
                        inGuid = guid;
                        taskFactory.StartNew(() => { clientReceive(inClient, guid); });
                    }
                }
                catch (Exception ex)
                {
                    inClient = null;
                    //Console.WriteLine("本地远程桌面服务连接失败，稍后将重连... {0}", ex.Message);
                }
                Thread.Sleep(2000);
            } while (true);

        }
        Task checkDeskConnectedTask = null;
        private void clientReceive(TcpClient client, Guid curGuid)
        {
            bool isLocalClient = client == inClient;
            NetworkStream ss = null;
            while (client != null && curGuid == (isLocalClient ? inGuid : outGuid))
            {
                byte[] recBytes = new byte[10240];
                int count = 0;
                try
                {
                    count = client.Client.Receive(recBytes);
                }
                catch (Exception ex)
                {
                    TcpClient tempClient = inClient;
                    if (isLocalClient)
                    {
                        inClient = null;
                        tempClient.Close();
                    }
                    else
                    {
                        outClient = null;
                    }
                    try
                    {
                        tempClient.Close();
                    }
                    catch { }
                    //Console.WriteLine("接收数据异常：{0}", ex);
                    break;
                }
                if (count > 0)
                {
                    if (!isLocalClient)
                    {
                        if (checkDeskConnectedTask == null)
                            checkDeskConnectedTask = taskFactory.StartNew(() => { CheckDeskConnected(inGuid); });
                    }
                    else
                    {
                        //远程桌面发送了数据
                        lastReceiveTime = DateTime.Now;
                    }
                    //Console.WriteLine("从{0}接收到数据,长度：{1}", client.Client.RemoteEndPoint,count);
                    TcpClient toClient = isLocalClient ? outClient : inClient;
                    if (toClient != null)
                    {
                        //转发数据
                        try
                        {
                            ss = ss == null ? toClient.GetStream() : ss;// Client.Send(recBytes);
                            ss.WriteAsync(recBytes, 0, count);
                        }
                        catch (Exception ex)
                        {
                            TcpClient tempClient = outClient;
                            if (isLocalClient)
                            {
                                //Console.WriteLine("向服务器发送数据失败！{0}", ex);
                                outClient = null;
                            }
                            else
                            {
                                //Console.WriteLine("向本地远程桌面服务发送数据失败！{0}", ex);
                                inClient = null;
                            }
                            try
                            {
                                tempClient.Close();
                            }
                            catch { }
                        }
                    }
                }
                client = isLocalClient ? inClient : outClient;
            }
        }

        private void CheckDeskConnected(Guid curGuid)
        {
            //Console.WriteLine("启动本地远程桌面服务通讯守护线程！");
            lastReceiveTime = DateTime.Now;
            TcpClient tempClient = inClient;
            while (curGuid == inGuid && inClient != null)
            {
                if ((DateTime.Now - lastReceiveTime).Milliseconds > 3000)
                {
                    //Console.WriteLine("本地远程桌面服务连接超时！");
                    lastReceiveTime = DateTime.Now;
                    inClient = null;
                    break;
                }
                Thread.Sleep(500);
            }
            try
            {
                tempClient.Close();
            }
            catch { }
            //Console.WriteLine("本地远程桌面服务守护线程退出！");
            checkDeskConnectedTask = null;
        }
    }
}
