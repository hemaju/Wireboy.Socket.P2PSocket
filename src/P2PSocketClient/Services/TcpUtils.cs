using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Wireboy.Socket.P2PClient.Models;

namespace Wireboy.Socket.P2PClient
{
    public static class TcpUtils
    {
        /// <summary>
        /// 异步发送数据
        /// </summary>
        /// <param name="client"></param>
        /// <param name="bytes">要发送的数据</param>
        /// <param name="msgType">消息类型</param>
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

        /// <summary>
        /// 异步发送数据
        /// </summary>
        /// <param name="client"></param>
        /// <param name="bytes">byte数组</param>
        /// <param name="length">数据实际长度</param>
        /// <param name="msgType">消息类型</param>
        /// <returns></returns>
        public static bool WriteAsync(this TcpClient client, byte[] bytes, int length, MsgType msgType)
        {
            return client.WriteAsync(bytes.Take(length).ToArray(), msgType);
        }

        /// <summary>
        /// 异步发送数据
        /// </summary>
        /// <param name="client"></param>
        /// <param name="str">要发送的文本</param>
        /// <param name="msgType">消息类型</param>
        /// <returns></returns>
        public static bool WriteAsync(this TcpClient client, string str, MsgType msgType)
        {
            return client.WriteAsync(Encoding.Unicode.GetBytes(str), msgType);
        }

        /// <summary>
        /// 将字符串转成byte数组
        /// </summary>
        /// <param name="str">字符串</param>
        /// <param name="tcpP2PType">tcp打洞通讯类型</param>
        /// <returns></returns>
        public static byte[] ToBytes(this string str, byte msgType)
        {
            List<byte> bytes = Encoding.Unicode.GetBytes(str).ToList();
            bytes.Insert(0, msgType);
            return bytes.ToArray();
        }

        /// <summary>
        /// 将byte数组转成字符串
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static String ToStringUnicode(this byte[] data)
        {
            return Encoding.Unicode.GetString(data);
        }

        /// <summary>
        /// 将byte数组转成字符串
        /// </summary>
        /// <param name="data">byte数组</param>
        /// <param name="startIndex">起始位置</param>
        /// <returns></returns>
        public static String ToStringUnicode(this byte[] data, int startIndex)
        {
            return data.Skip(startIndex).ToArray().ToStringUnicode();
        }
    }
}
