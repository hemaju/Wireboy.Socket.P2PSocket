using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace P2PSocektLib
{
    public interface INetworkConnect
    {
        /// <summary>
        /// 获取远端地址
        /// </summary>
        /// <returns></returns>
        IPAddress GetRemoteAddress();
        /// <summary>
        /// 获取本地地址
        /// </summary>
        /// <returns></returns>
        IPAddress GetLocalAddress();
        /// <summary>
        /// 关闭连接
        /// </summary>
        void Close();
        /// <summary>
        /// 建立连接
        /// </summary>
        void Connect(string ip, int port);
        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data"></param>
        Task SendData(byte[] data, int length);
        /// <summary>
        /// 接收数据
        /// </summary>
        /// <param name="data"></param>
        Task<int> ReadData(byte[] data, int length);
    }
}
