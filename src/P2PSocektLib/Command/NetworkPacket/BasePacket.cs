using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using P2PSocektLib.Command;

namespace P2PSocektLib
{ 
    internal class BasePacket : INetworkPacket
    {
        protected byte[] packetBuffer = new byte[4];
        protected byte[] data = new byte[0];
        public RequestEnum RequestType { set; get; }
        protected INetworkConnect Conn { set; get; }
        public BasePacket(INetworkConnect conn)
        {
            Conn = conn;
        }

        protected static byte[] PackOne(byte[] data)
        {
            byte[] packet = new byte[data.Length + 2 + 4 + 1 + 2 + 1];
            // 写入包头
            packet[0] = packet[1] = 55;
            // 写入长度
            BitConverter.GetBytes((ushort)data.Length).CopyTo(packet, 2);
            // 写入数据
            data.CopyTo(packet, 4);
            // 写入包尾
            packet[packet.Length - 1] = 77;
            return packet;
        }

        /// <summary>
        /// 读取一个数据包
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public virtual async Task<byte[]> ReadOne()
        {
            // 读取包头
            int length = await Conn.ReadData(packetBuffer, 2);
            if (length != 2) throw new Exception("读取数据失败");
            if (packetBuffer[0] != 55 || packetBuffer[1] != 55)
            {
                throw new Exception("数据包校验失败");
            }

            // 读取数据长度
            length = await Conn.ReadData(packetBuffer, 2);
            if (length != 2)
                throw new Exception("读取数据失败");
            int dataLength = BitConverter.ToInt16(packetBuffer);

            // 读取数据
            data = new byte[dataLength];
            length = 0;
            while (length < dataLength)
            {
                length += await Conn.ReadData(data, dataLength);
                if (length == 0)
                {
                    throw new Exception("读取数据失败");
                }
            }

            // 读取包尾
            length = await Conn.ReadData(packetBuffer, 1);
            if (length != 1)
                throw new Exception("读取数据失败");
            if (packetBuffer[0] != 77)
                throw new Exception("数据包校验失败");
            return data;
        }
    }
}
