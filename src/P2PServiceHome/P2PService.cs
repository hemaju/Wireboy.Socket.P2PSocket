using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using Wireboy.Socket.P2PHome;
using Wireboy.Socket.P2PHome.Services;

namespace P2PServiceHome
{
    public class P2PService
    {
        /// <summary>
        /// 仅用于线程创建
        /// </summary>
        TaskFactory taskFactory = new TaskFactory();
        /// <summary>
        /// 本地Tcp连接
        /// </summary>
        TcpClient LocalTcp { set; get; }
        /// <summary>
        /// 服务器Tcp连接
        /// </summary>
        TcpClient ServerTcp { set; get; }
        public P2PService()
        {
        }
        public void Start()
        {
            Console.WriteLine("正在连接服务器...");
            while (ServerTcp == null)
            {
                try
                {
                    ServerTcp = new TcpClient(ConfigServer.AppSettings.ServerIp, ConfigServer.AppSettings.ServerPort);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("{0}", ex);
                    Logger.Write("{0}", ex);
                    Console.WriteLine("1秒后重新连接.");
                    ServerTcp = null;
                    Thread.Sleep(1000);
                }
            }
            Console.WriteLine("成功连接服务器！");
            while (string.IsNullOrEmpty(ConfigServer.AppSettings.ServerName))
            {
                Console.WriteLine("请输入服务名称：");
                ConfigServer.AppSettings.ServerName = Console.ReadLine();
            }
            Console.WriteLine(string.Format("当前服务名称：{0}", ConfigServer.AppSettings.ServerName));
            Logger.Write("当前服务名称：{0}", ConfigServer.AppSettings.ServerName);

        }

        
    }
}
