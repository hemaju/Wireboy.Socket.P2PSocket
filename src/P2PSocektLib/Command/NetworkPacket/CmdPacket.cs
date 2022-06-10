using P2PSocektLib.Command;
using P2PSocektLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PSocektLib
{
    internal class CmdPacket : INetworkPacket
    {
        byte[] packetBuffer = new byte[4];
        byte[] data = new byte[0];
        public uint Token { set; get; }
        public RequestEnum RequestType { set; get; }
        INetworkConnect Conn { set; get; }
        public CmdPacket(INetworkConnect conn)
        {
            Conn = conn;
        }

        public static byte[] PackOne(byte[] data, uint token, RequestEnum cmdType)
        {
            byte[] packet = new byte[data.Length + 2 + 4 + 1 + 2 + 1];
            // 写入包头
            packet[0] = packet[1] = 55;
            // 写入token
            BitConverter.GetBytes(token).CopyTo(packet, 2);
            // 写入命令
            packet[6] = (byte)cmdType;
            // 写入长度
            BitConverter.GetBytes((ushort)data.Length).CopyTo(packet, 7);
            // 写入数据
            data.CopyTo(packet, 9);
            // 写入包尾
            packet[packet.Length - 1] = 77;
            return packet;
        }

        /// <summary>
        /// 读取一个数据包
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<byte[]> ReadOne()
        {
            // 读取包头
            int length = await Conn.ReadData(packetBuffer, 2);
            if (length != 2) throw new Exception("读取数据失败");
            if (packetBuffer[0] != 55 || packetBuffer[1] != 55)
            {
                throw new Exception("数据包校验失败");
            }
            // 读取token
            length = await Conn.ReadData(packetBuffer, 4);
            if (length != 4) throw new Exception("数据读取失败");
            Token = BitConverter.ToUInt32(packetBuffer);

            // 读取命令类型
            length = await Conn.ReadData(packetBuffer, 1);
            if (length != 1) throw new Exception("读取数据失败");
            RequestType = (RequestEnum)packetBuffer[0];

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
