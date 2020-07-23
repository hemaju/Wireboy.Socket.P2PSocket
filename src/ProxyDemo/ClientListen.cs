using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ProxyDemo
{
    public class ClientListen
    {
        TcpListener socksListener = null;
        TcpListener httpListener = null;
        TaskFactory task = new TaskFactory();
        public ClientListen()
        {
            socksListener = new TcpListener(IPAddress.Any, 13520);
            httpListener = new TcpListener(IPAddress.Any, 13521);
            Console.WriteLine("socks代理地址：127.0.0.1:13520");
            Console.WriteLine("http代理地址：127.0.0.1:13521");
            TaskFactory taskFactory = new TaskFactory();
            taskFactory.StartNew(() => { StartSocksProxyListen(); });
            taskFactory.StartNew(() => { StartHttpProxyListen(); });
        }

        void StartHttpProxyListen()
        {
            httpListener.Start();
            while (true)
            {
                TcpClient socket = httpListener.AcceptTcpClient();
                //Console.WriteLine($"tcp:{socket.Client.RemoteEndPoint.ToString()}");

                task.StartNew(() => {
                    try
                    {
                        HandleHttpProxy(socket);
                    }
                    catch (Exception)
                    {
                        socket.Close();
                    }
                });
                //HandleHttpProxy(socket);
            }
        }

        void HandleHttpProxy(TcpClient socket)
        {
            NetworkStream fromSt = socket.GetStream();

            byte[] data = new byte[10240];
            int length = fromSt.Read(data);


            //http的请求内容
            string httpRequest = Encoding.UTF8.GetString(data.Take(length).ToArray());
            //分行
            string[] list = httpRequest.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            //请求的地址
            string dest = list.Where(t => t.StartsWith("Host:")).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(dest))
            {
                dest = dest.Replace("Host:", "").Trim();
                string[] ipDest = dest.Split(':');
                string domain = "";
                int port = 80;
                if (ipDest.Length > 0)
                {
                    domain = ipDest[0];
                    if (ipDest.Length > 1)
                    {
                        port = Convert.ToInt32(ipDest[1]);
                    }
                }



                //Console.WriteLine($"地址：{domain}:{port}");
                if (httpRequest.StartsWith("CONNECT"))
                {
                    //Console.WriteLine("不支持https协议");
                    //socket.Close();
                    TcpClient desttcp = new TcpClient(domain, port);

                    //Console.WriteLine($"连接成功");
                    socket.GetStream().Write(Encoding.UTF8.GetBytes("HTTP/1.1 200 Connection Established\r\n\r\n"));
                    task.StartNew(() => { bindTcp(socket, desttcp); });
                    task.StartNew(() => { bindTcp(desttcp, socket); });
                }
                else
                {
                    TcpClient desttcp = new TcpClient(domain, port);
                    task.StartNew(() => { bindTcp(socket, desttcp); });
                    task.StartNew(() => { bindTcp(desttcp, socket); });
                    desttcp.GetStream().Write(data.Take(length).ToArray());
                }
                //Console.WriteLine("数据：\r\n{0}", Encoding.UTF8.GetString(data.Take(length).ToArray()));

                //desttcp.GetStream().Write(data.Take(length).ToArray());
                //desttcp.GetStream().Write(Encoding.UTF8.GetBytes(httpRequest.Replace("CONNECT","GET")));
            }
            //socket.Close();
        }

        void StartSocksProxyListen()
        {
            socksListener.Start();
            while (true)
            {
                TcpClient socket = socksListener.AcceptTcpClient();
                task.StartNew(() => {
                    try
                    {
                        HandleSocks5Proxy(socket);
                    }
                    catch(Exception ex)
                    {
                        //Console.WriteLine(ex.ToString());
                        socket.Close();
                    }
                });

            }
        }

        void HandleSocks5Proxy(TcpClient socket)
        {
            bool isSuccess = false;
            socket.NoDelay = true;
            //Console.WriteLine($"tcp:{socket.Client.RemoteEndPoint.ToString()}");
            NetworkStream fromSt = socket.GetStream();
            byte[] data = new byte[10240];
            int length = fromSt.Read(data);
            int sType = data[0];
            //Console.WriteLine($"sType数据：{sType}");
            //只处理Socks5协议
            if (sType == 0x05)
            {
                fromSt.Write(new byte[] { 0x05, 0x00 });
                length = fromSt.Read(data);
                int type = data[3];
                //Console.WriteLine($"type数据：{type}");
                string address = "";
                int port = 80;
                switch (type)
                {
                    case 0x01:
                        {
                            address = String.Format("{0}.{1}.{2}.{3}", data[4], data[5], data[6], data[7]);
                            port = BitConverter.ToUInt16(data.Skip(8).Take(2).Reverse().ToArray());
                            //Console.WriteLine("目标地址1：{0}", String.Format("{0}:{1}", address, port));
                            isSuccess = true;
                            break;
                        }
                    case 0x03:
                        {
                            address = Encoding.UTF8.GetString(data.Skip(5).Take(length - 5 - 2).ToArray());
                            port = BitConverter.ToUInt16(data.Skip(length - 2).Take(2).Reverse().ToArray());
                            //Console.WriteLine("目标地址2：{0}", String.Format("{0}:{1}", address, port));
                            isSuccess = true;
                            IPAddress iPAddress = Dns.GetHostEntry(address).AddressList.FirstOrDefault();
                            //Console.WriteLine(iPAddress.ToString());
                            address = iPAddress.ToString();
                            break;
                        }
                }
                if (isSuccess)
                {
                    //Console.WriteLine("实际地址：{0}", String.Format("{0}:{1}", address, port));
                    TcpClient desttcp = null;
                    if (address == "0.0.0.0")
                    {
                        desttcp = new TcpClient(new IPEndPoint(IPAddress.Any,port));
                    }
                    else
                        desttcp = new TcpClient(address,port);
                    task.StartNew(() => { bindTcp(socket, desttcp); });
                    task.StartNew(() => { bindTcp(desttcp, socket); });
                    fromSt.Write(new byte[] { 0x05, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                }

            }
        }


        private void bindTcp(TcpClient readClient, TcpClient writeClient)
        {
            //if (writeClient == null || !writeClient.Connected)
            //{
            //    Console.WriteLine($"数据转发失败：绑定的Tcp连接已断开.");
            //    readClient.Close();
            //    return;
            //}
            byte[] buffer = new byte[10240];
            try
            {
                NetworkStream readStream = readClient.GetStream();
                NetworkStream toStream = writeClient.GetStream();
                while (readClient.Connected)
                {
                    int curReadLength = readStream.Read(buffer, 0, buffer.Length);
                    if (curReadLength > 0)
                    {
                        //Console.WriteLine("接收的数据：\r\n{0}", Encoding.UTF8.GetString(buffer.Take(curReadLength).ToArray()));
                        toStream.Write(buffer, 0, curReadLength);
                    }
                    else
                    {
                        Console.WriteLine($"源Tcp连接已断开.");
                        //如果tcp已关闭，需要关闭相关tcp
                        try
                        {
                            writeClient.Close();
                        }
                        finally
                        {
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"端口映射转发（ip模式）：目标Tcp连接已断开.");
                readClient.Close();
            }
        }
    }
}
