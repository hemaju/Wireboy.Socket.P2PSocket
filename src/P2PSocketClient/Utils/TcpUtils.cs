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
        public static byte StartCode { get; } = 86;
        public static void WriteAsync(this TcpClient client, byte[] bytes)
        {
            NetworkStream networkStream = client.GetStream();
            if (networkStream.CanWrite)
            {
                networkStream.WriteAsync(bytes, 0, bytes.Length);
            }
        }

        public static void WriteAsync(this TcpClient client, byte[] bytes, int length, byte type1, byte type2 = 0)
        {
            short dataLength = Convert.ToInt16(length);
            List<byte> sendData = new List<byte>() { StartCode, StartCode, type1, type2 };
            sendData.AddRange(BitConverter.GetBytes(dataLength));
            sendData.AddRange(bytes.Take(length));
            client.WriteAsync(sendData.ToArray());
        }
        public static void WriteAsync(this TcpClient client, byte[] bytes, byte type1, byte type2 = 0)
        {
            int length = bytes.Length;
            short dataLength = Convert.ToInt16(length);
            List<byte> sendData = new List<byte>() { StartCode, StartCode, type1, type2 };
            sendData.AddRange(BitConverter.GetBytes(dataLength));
            sendData.AddRange(bytes.Take(length));
            client.WriteAsync(sendData.ToArray());
        }

        public static void WriteAsync(this TcpClient client, string str, byte type1, byte type2 = 0)
        {
            byte[] data = Encoding.Unicode.GetBytes(str);
            short dataLength = Convert.ToInt16(data.Length);
            List<byte> sendData = new List<byte>() { StartCode, StartCode, type1, type2 };
            sendData.AddRange(BitConverter.GetBytes(dataLength));
            sendData.AddRange(data);
            client.WriteAsync(sendData.ToArray());
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
        public static byte[] ToBytes(this string str)
        {
            List<byte> bytes = Encoding.Unicode.GetBytes(str).ToList();
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
    }
}
