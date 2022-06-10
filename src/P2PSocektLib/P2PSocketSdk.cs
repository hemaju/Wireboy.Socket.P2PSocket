using P2PSocektLib.Export;

namespace P2PSocektLib
{
    public class P2PSocketSdk
    {
        /// <summary>
        /// 新建服务端实例
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public IP2PServer CreateServer(int port)
        {
            return new P2PServer(port);
        }

        /// <summary>
        /// 新建客户端实例
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public IP2PClient CreateClient(string host, int port)
        {
            IP2PClient client = new P2PClient(host, port);
            return client;
        }
    }
}