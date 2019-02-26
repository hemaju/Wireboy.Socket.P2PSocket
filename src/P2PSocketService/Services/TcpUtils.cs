using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using Wireboy.Socket.P2PService.Models;

namespace Wireboy.Socket.P2PService.Services
{
    public static class TcpUtils
    {
        /// <summary>
        /// 异步发送数据
        /// </summary>
        /// <param name="client"></param>
        /// <param name="bytes">要发送的数据</param>
        /// <param name="msgType">数据类型（转发数据不自动封包）</param>
        /// <returns></returns>
        public static bool WriteAsync(this TcpClient client,byte[] bytes, MsgType msgType)
        {
            NetworkStream networkStream = client.GetStream();
            if(networkStream.CanWrite)
            {
                if (msgType == MsgType.数据转发)
                {
                    //数据转发则不处理数据
                    networkStream.WriteAsync(bytes);
                }
                else
                {
                    short dataLength = Convert.ToInt16(bytes.Length + 1);
                    byte[] sendBytes = new byte[2 + bytes.Length + 1];
                    BitConverter.GetBytes(dataLength).CopyTo(sendBytes, 0);
                    sendBytes[2] = (byte)msgType;
                    bytes.CopyTo(sendBytes, 3);
                    networkStream.WriteAsync(sendBytes);
                }
            }
            else
            {
                throw new Exception("当前tcp数据流不可写入！");
            }
            return true;
        }
    }
}
