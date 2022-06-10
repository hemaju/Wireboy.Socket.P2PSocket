using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace P2PSocektLib.Network.Model
{
    public class P2PTcpListener : INetworkListener
    {
        TcpListener? tcpListener = null;
        AcceptConnectionEventCallback? acceptConnectionEventCallback = null;
        public P2PTcpListener()
        {

        }
        public void Bind(int port)
        {
            tcpListener = new TcpListener(System.Net.IPAddress.Any, port);
        }

        public void BindAcceptConnectionEvent(AcceptConnectionEventCallback action)
        {
            acceptConnectionEventCallback = action;
        }

        public void Start()
        {
            tcpListener.Start();
            // 准备处理连入的tcp
            AcceptConnect();
        }

        public void Stop()
        {
            tcpListener.Stop();
        }

        /// <summary>
        /// 开始接收连入消息
        /// </summary>
        private async void AcceptConnect()
        {
            try
            {
                do
                {
                    if (acceptConnectionEventCallback != null)
                    {
                        TcpClient client = await tcpListener.AcceptTcpClientAsync();
                        acceptConnectionEventCallback(new P2PTcpConnect(client));
                    }
                    else await Task.Delay(100);

                } while (true);
            }
            catch
            {
                // 一般这里出错都是关闭了监听
            }
        }
    }
}
