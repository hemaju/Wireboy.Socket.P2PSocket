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
                NetworkStream fromSt = socket.GetStream();



                byte[] data = new byte[1024];
                int length = fromSt.Read(data);
                ////Console.WriteLine("数据：{0}", string.Join(" ", data.Select(t=>t.ToString("X2")).ToList()));
                //Console.WriteLine("数据:{0}",Encoding.UTF8.GetString(data.Take(length).ToArray()));


                //http的请求内容
                string httpRequest = Encoding.UTF8.GetString(data.Take(length).ToArray());
                //分行
                string[] list = httpRequest.Split(new string[] { "\r\n"}, StringSplitOptions.None);
                //请求的地址
                string dest =  list.Where(t => t.StartsWith("Host:")).FirstOrDefault();
                if(!string.IsNullOrWhiteSpace(dest))
                {
                    dest = dest.Replace("Host:","").Trim();
                    string[] ipDest = dest.Split(':');
                    string domain = "";
                    int port = 80;
                    if(ipDest.Length > 0)
                    {
                        domain = ipDest[0];
                        if (ipDest.Length > 1)
                        {
                            port = Convert.ToInt32(ipDest[1]);
                        }
                    }
                    Console.WriteLine($"{domain}:{port}");
                }

                socket.Close();

            }
        }
        void StartSocksProxyListen()
        {
            socksListener.Start();
            while (true)
            {
                TcpClient socket = socksListener.AcceptTcpClient();
                Console.WriteLine($"tcp:{socket.Client.RemoteEndPoint.ToString()}");
                NetworkStream fromSt = socket.GetStream();



                //byte[] data = new byte[1024];
                //int length = fromSt.Read(data);
                ////Console.WriteLine("数据：{0}", string.Join(" ", data.Select(t=>t.ToString("X2")).ToList()));
                //Console.WriteLine("数据:{0}",Encoding.UTF8.GetString(data.Take(length).ToArray()));



                int sType = fromSt.ReadByte();
                Console.WriteLine($"数据：{sType}");
                //只处理Socks5协议
                if (sType == 0x05)
                {
                    fromSt.WriteByte(0x05);
                    fromSt.WriteByte(0x00);
                    fromSt.Seek(2, System.IO.SeekOrigin.Current);
                    int type = fromSt.ReadByte();
                    Console.WriteLine($"数据：{type}");
                    switch (type)
                    {
                        case 0x01:
                            {
                                byte[] data = new byte[1024];
                                int length = fromSt.Read(data);
                                Console.WriteLine("目标地址：{0}", data.Take(length).ToString());
                                break;
                            }
                        case 0x03:
                            {
                                byte[] data = new byte[1024];
                                int length = fromSt.Read(data);
                                Console.WriteLine("目标地址：{0}  目标端口：{1}", data.Take(length - 2).ToString(), data.Skip(length - 2).Take(2).ToString());
                                break;
                            }
                    }
                }
                socket.Close();
                
            }
        }
    }
}
