using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using Wireboy.Socket.P2PService.Models;
using System.Linq;

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
        public static bool WriteAsync(this TcpClient client, byte[] bytes, MsgType msgType)
        {
            NetworkStream networkStream = client.GetStream();
            if (networkStream.CanWrite)
            {
                if (msgType == MsgType.不封包)
                {
                    networkStream.WriteAsync(bytes, 0, bytes.Length);
                }
                else
                {
                    short dataLength = Convert.ToInt16(bytes.Length + 1);
                    byte[] sendBytes = new byte[2 + bytes.Length + 1];
                    BitConverter.GetBytes(dataLength).CopyTo(sendBytes, 0);
                    sendBytes[2] = (byte)msgType;
                    bytes.CopyTo(sendBytes, 3);
                    networkStream.WriteAsync(sendBytes, 0, sendBytes.Length);
                }
            }
            else
            {
                throw new Exception("当前tcp数据流不可写入！");
            }
            return true;
        }
        public static bool WriteAsync(this TcpClient client, byte[] bytes, int length, MsgType msgType)
        {
            return client.WriteAsync(bytes.Take(length).ToArray(), msgType);
        }

        public static bool WriteAsync(this TcpClient client, string str, MsgType msgType)
        {
            return client.WriteAsync(Encoding.Unicode.GetBytes(str), msgType);
        }

        public static String ToStringUnicode(this byte[] data)
        {
            return Encoding.Unicode.GetString(data);
        }
        public static String ToStringUnicode(this byte[] data, int startIndex)
        {
            return data.Skip(startIndex).ToArray().ToStringUnicode();
        }
    }
}
