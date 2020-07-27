using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
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
            StartSocksProxyListen();
            StartHttpProxyListen();
        }

        void StartHttpProxyListen()
        {
            httpListener.Start();
            httpListener.BeginAcceptTcpClient(AcceptSocket_http, httpListener);
        }

        void AcceptSocket_http(IAsyncResult ar)
        {
            TcpListener listen = (TcpListener)ar.AsyncState;
            TcpClient socket = null;
            try
            {
                socket = listen.EndAcceptTcpClient(ar);
            }
            catch (Exception ex)
            {
                return;
            }
            listen.BeginAcceptTcpClient(AcceptSocket_http, listen);
            HandleHttpProxy(socket);
        }
        void AcceptSocket_socks5(IAsyncResult ar)
        {
            TcpListener listen = (TcpListener)ar.AsyncState;
            TcpClient socket = null;
            try
            {
                socket = listen.EndAcceptTcpClient(ar);
            }
            catch (Exception ex)
            {
                return;
            }
            listen.BeginAcceptTcpClient(AcceptSocket_socks5, listen);
            HandleSocks5Proxy(socket);
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
                try
                {
                    if (httpRequest.StartsWith("CONNECT"))
                    {
                        TcpClient desttcp = new TcpClient(domain, port);

                        //Console.WriteLine($"连接成功");
                        socket.GetStream().Write(Encoding.UTF8.GetBytes("HTTP/1.1 200 Connection Established\r\n\r\n"));
                        bindTcpClient(socket, desttcp);
                    }
                    else
                    {
                        TcpClient desttcp = new TcpClient(domain, port);

                        bindTcpClient(socket, desttcp);
                        desttcp.GetStream().Write(data.Take(length).ToArray());
                    }
                }
                catch
                {
                    socket.Close();
                }
                //Console.WriteLine("数据：\r\n{0}", Encoding.UTF8.GetString(data.Take(length).ToArray()));
                //desttcp.GetStream().Write(data.Take(length).ToArray());
                //desttcp.GetStream().Write(Encoding.UTF8.GetBytes(httpRequest.Replace("CONNECT","GET")));
            }
        }

        void StartSocksProxyListen()
        {
            socksListener.Start();
            socksListener.BeginAcceptTcpClient(AcceptSocket_socks5, socksListener);
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
                            try
                            {
                                IPAddress iPAddress = Dns.GetHostEntry(address).AddressList.FirstOrDefault();
                                //Console.WriteLine(iPAddress.ToString());
                                address = iPAddress.ToString();
                                isSuccess = true;
                            }
                            catch
                            {
                            }
                            break;
                        }
                }
                if (isSuccess)
                {
                    //Console.WriteLine("实际地址：{0}", String.Format("{0}:{1}", address, port));
                    TcpClient desttcp = null;
                    try
                    {
                        if (address == "0.0.0.0")
                        {
                            desttcp = new TcpClient(new IPEndPoint(IPAddress.Any, port));
                        }
                        else
                            desttcp = new TcpClient(address, port);
                        bindTcpClient(socket, desttcp);
                        if (fromSt.CanWrite)
                            fromSt.Write(new byte[] { 0x05, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                    }
                    catch
                    {
                        socket.Close();
                    }
                }
                else
                {
                    socket.Close();
                }

            }
        }

        private void bindTcpClient(TcpClient readClient, TcpClient writeClient)
        {
            try
            {
                NetworkStream readSs = readClient.GetStream();
                NetworkStream writeSs = writeClient.GetStream();
                RelationTcp romrRelation = new RelationTcp() { readTcp = readClient, readSs = readSs, writeTcp = writeClient, writeSs = writeSs, buffer = new byte[10240] };
                RelationTcp toRelation = new RelationTcp() { readTcp = writeClient, readSs = writeSs, writeTcp = readClient, writeSs = readSs, buffer = new byte[10240] };
                StartRead(romrRelation);
                StartRead(toRelation);
            }
            catch
            {
                try
                {
                    readClient.Close();
                }
                catch { }
                try
                {
                    writeClient.Close();
                }
                catch { }
                return;
            }

        }

        private void StartRead(RelationTcp tcp)
        {
            tcp.readTcp.GetStream().BeginRead(tcp.buffer, 0, tcp.buffer.Length, ReadTcp, tcp);
        }

        private void ReadTcp(IAsyncResult ar)
        {
            RelationTcp relation = (RelationTcp)ar.AsyncState;

            if (relation.readSs.CanRead)
            {
                int length = 0;
                try
                {
                    length = relation.readSs.EndRead(ar);
                }
                catch { }
                if (length > 0)
                {
                    if (relation.writeSs.CanWrite)
                    {
                        try
                        {
                            relation.writeSs.Write(relation.buffer.Take(length).ToArray());
                            StartRead(relation);
                            return;
                        }
                        catch
                        {
                            Console.WriteLine("连接中断");
                        }
                    }
                }
            }
            relation.readSs.Close(3000);
            relation.writeSs.Close(3000);
            relation.readTcp.Close();
            relation.writeTcp.Close();
        }

    }

    public struct RelationTcp
    {
        public TcpClient readTcp;
        public TcpClient writeTcp;
        public NetworkStream readSs;
        public NetworkStream writeSs;
        public byte[] buffer;
    }
}
