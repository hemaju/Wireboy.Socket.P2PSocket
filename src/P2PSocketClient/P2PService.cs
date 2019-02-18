using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Wireboy.Socket.P2PClient
{
    public class P2PService
    {
        string service_IpAddress = "39.105.115.162";
        int service_Port = 3388;
        public P2PService()
        {
        }
        public void Start()
        {
            IPAddress ip = new IPAddress(new byte[] { 127, 0, 0, 1 });
            TcpListener tl = new TcpListener(IPAddress.Any, 3388);//这里开对方可以被你连接并且未被占用的端口  
            tl.Start();


            Console.WriteLine("监听中... 端口：{0}","3388");

            TcpClient outClient = new TcpClient(service_IpAddress,service_Port);
            //outClient.Client.Connect(service_IpAddress, service_Port);

            NetworkStream ssOut = outClient.GetStream();

            List<byte> sMsg = null;

            Console.WriteLine("请输入要连接的服务名称：");
            string str = Console.ReadLine();
            sMsg = Encoding.ASCII.GetBytes(str).ToList();
            sMsg.Insert(0, 56);
            ssOut.Write(sMsg.ToArray(), 0, sMsg.ToArray().Length);
            //while (true)
            {
                TcpClient inClient = tl.AcceptTcpClient();
                Console.WriteLine(string.Format("新联入主机：{0}",inClient.Client.RemoteEndPoint));
                Console.WriteLine(string.Format("数据转发至：{0}", outClient.Client.RemoteEndPoint));
                TaskFactory taskFactory = new TaskFactory();
                List<Task> taskList = new List<Task>();
                taskList.Add(taskFactory.StartNew(() => { Transfer(outClient, inClient); }));
                taskList.Add(taskFactory.StartNew(() => { Transfer(inClient, outClient); }));
                Task.WaitAll(taskList.ToArray());
            }
        }

        public void Transfer(TcpClient outClient, TcpClient inClient)
        {
            NetworkStream ssOut = outClient.GetStream();
            NetworkStream ssIn = inClient.GetStream();
            try
            {
                while (true)
                {
                    byte[] bytes = new byte[10240];
                    int count = ssIn.Read(bytes, 0, bytes.Length);
                    if (count > 0)
                    {
                        //Console.WriteLine(string.Format("{0} -> {1} - 长度：{2}", inClient.Client.RemoteEndPoint, outClient.Client.RemoteEndPoint,count));
                        ssOut.WriteAsync(bytes, 0, count);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("{0} - {1}",inClient.Client.RemoteEndPoint,ex.Message));
            }
        }
    }
}
