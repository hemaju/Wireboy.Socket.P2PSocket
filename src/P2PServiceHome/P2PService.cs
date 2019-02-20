using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using Wireboy.Socket.P2PHome;

namespace P2PServiceHome
{
    public class P2PService
    {
        string service_IpAddress = "39.105.115.162";
        int service_Port = 3388;
        string ServerName = "MyPC";
        List<Task> taskList = new List<Task>();

        TcpClient outClient = null;
        TcpClient inClient = null;
        //从mstsc接收到数据的时间
        DateTime recFromMstscTime = DateTime.Now;
        //从服务器接收到数据的时间
        DateTime recFromServiceTime = DateTime.Now;
        TcpClient heartClient = null;
        Guid inGuid = Guid.NewGuid();
        Guid outGuid = Guid.NewGuid();
        TaskFactory taskFactory = new TaskFactory();
        public P2PService()
        {
        }
        public void Start()
        {
            recFromMstscTime = DateTime.Now;
            do
            {
                Console.WriteLine("请输入当前服务名称");
                ServerName = Console.ReadLine();
            } while (string.IsNullOrEmpty(ServerName));
            Console.WriteLine(string.Format("当前服务名称：{0}", ServerName));
            Logger.Write("当前服务名称：{0}", ServerName);
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
                    Logger.Write("服务器连接失败，稍后将重连... {0}", ex);
                }
                try
                {
                    if (outClient == null || !outClient.Connected)
                    {
                        Guid guid = Guid.NewGuid();
                        Logger.Write("正在连接服务器... {0}:{1}", service_IpAddress, service_Port);
                        outClient = new TcpClient(service_IpAddress, service_Port);
                        Logger.Write("服务器成功连接");

                        NetworkStream ssOut = outClient.GetStream();
                        List<byte> sMsg = Encoding.ASCII.GetBytes(ServerName).ToList();
                        sMsg.Insert(0, 55);
                        ssOut.Write(sMsg.ToArray(), 0, sMsg.ToArray().Length);
                        outGuid = guid;
                        taskFactory.StartNew(() => { clientReceive(outClient, guid); });
                    }
                }
                catch (Exception ex)
                {
                    outClient = null;
                    Logger.Write("服务器连接失败，稍后将重连... {0}", ex);
                }
                try
                {
                    if (inClient == null || !inClient.Connected)
                    {
                        Guid guid = Guid.NewGuid();
                        Logger.Write("正在连接本地远程桌面服务... 127.0.0.1:3389");
                        inClient = new TcpClient();
                        inClient.Connect(IPAddress.Parse("127.0.0.1"), 3389);
                        Logger.Write("本地远程桌面服务连接成功");
                        inGuid = guid;
                        taskFactory.StartNew(() => { clientReceive(inClient, guid); });
                    }
                }
                catch (Exception ex)
                {
                    inClient = null;
                    Logger.Write("本地远程桌面服务连接失败，稍后将重连... {0}", ex.Message);
                }
                Thread.Sleep(2000);
            } while (true);

        }
        Task checkDeskConnectedTask = null;
        Task checkServieceConnectedTask = null;
        private void clientReceive(TcpClient client, Guid curGuid)
        {
            bool isLocalClient = client == inClient;
            Guid curOutGuid = isLocalClient ? outGuid : inGuid;
            if (isLocalClient)
            {
                Logger.Write("启动本地远程桌面数据接收线程...");
            }
            else
            {
                Logger.Write("启动服务器数据接收线程....");
            }
            NetworkStream ss = null;
            byte[] recBytes = new byte[10240];
            while (client != null && curGuid == (isLocalClient ? inGuid : outGuid))
            {
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
                    Logger.Write("接收数据异常：{0}", ex);
                    break;
                }
                if (count > 0)
                {
                    if (!isLocalClient)
                    {
                        recFromServiceTime = DateTime.Now;
                        if (checkDeskConnectedTask == null)
                            checkDeskConnectedTask = taskFactory.StartNew(() => { CheckDeskConnected(inGuid); });
                        if (checkServieceConnectedTask == null)
                            checkServieceConnectedTask = taskFactory.StartNew(() => { CheckServiceConnected(inGuid); });
                    }
                    else
                    {
                        //远程桌面发送了数据
                        recFromMstscTime = DateTime.Now;
                    }
                    //Logger.Write("从{0}接收到数据,长度：{1}", client.Client.RemoteEndPoint,count);
                    TcpClient toClient = isLocalClient ? outClient : inClient;
                    if (toClient != null)
                    {
                        //转发数据
                        try
                        {
                            if (curOutGuid != (isLocalClient ? outGuid : inGuid))
                            {
                                ss = null;
                                curOutGuid = isLocalClient ? outGuid : inGuid;
                            }
                            ss = ss == null ? toClient.GetStream() : ss;
                            ss.WriteAsync(recBytes, 0, count);
                        }
                        catch (Exception ex)
                        {
                            TcpClient tempClient = null;
                            if (isLocalClient)
                            {
                                Logger.Write("向服务器发送数据失败！{0}", ex);
                                tempClient = outClient;
                                outClient = null;
                            }
                            else
                            {
                                Logger.Write("向本地远程桌面服务发送数据失败！{0}", ex);
                                tempClient = inClient;
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
            if (isLocalClient)
            {
                Logger.Write("退出本地远程桌面数据接收线程....");
            }
            else
            {
                Logger.Write("退出服务器数据接收线程....");
            }
        }

        private void CheckDeskConnected(Guid curGuid)
        {
            Logger.Write("接收来自本地远程桌面服务的数据，启动守护线程！");
            recFromMstscTime = DateTime.Now;
            TcpClient tempClient = inClient;
            while (curGuid == inGuid && inClient != null)
            {
                if ((DateTime.Now - recFromMstscTime).Milliseconds > 3000)
                {
                    Logger.Write("本地远程桌面服务连接超时！");
                    break;
                }
                Thread.Sleep(500);
            }
            try
            {
                tempClient.Close();
            }
            catch { }
            Logger.Write("本地远程桌面服务守护线程退出！");
            inClient = null;
            checkDeskConnectedTask = null;
        }

        private void CheckServiceConnected(Guid curGuid)
        {
            Logger.Write("接收来自服务器的数据，启动守护线程！");
            recFromServiceTime = DateTime.Now;
            TcpClient tempClient = inClient;
            while (curGuid == inGuid && inClient != null)
            {
                if ((DateTime.Now - recFromServiceTime).Milliseconds > 3000)
                {
                    Logger.Write("服务器连接超时！");
                    break;
                }
                Thread.Sleep(500);
            }
            try
            {
                tempClient.Close();
            }
            catch { }
            Logger.Write("服务器守护线程退出！");
            inClient = null;
            checkServieceConnectedTask = null;
        }
    }
}
