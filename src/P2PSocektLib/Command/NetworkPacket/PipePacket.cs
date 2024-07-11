using P2PSocektLib.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PSocektLib
{
    /// <summary>
    /// 管道通讯打包
    /// </summary>
    internal class PipePacket : BasePacket
    {
        public uint Token { set; get; }
        public bool IsRequest { set; get; }
        public PipePacket(INetworkConnect conn) : base(conn)
        {
        }

        public static byte[] PackOne(byte[] data, RequestEnum cmdType, uint token = 0, bool isRequest = true)
        {
            int offset = 0;
            // 数据+类型+是否token+token+是否Request
            //int dataLength = data.Length + 1 + 1 + 4 + 1;
            byte[] packet = token == 0 ? new byte[data.Length + 3] : new byte[data.Length + 7];
            // 命令
            packet[0] = (byte)cmdType;
            // 是否Request
            packet[1] = (byte)(isRequest ? 1 : 0);
            offset += 2;
            if (token == 0)
            {
                // 是否有token
                packet[offset] = 0;
                offset += 1;
            }
            else
            {
                // 是否有token
                packet[offset] = 1;
                offset += 1;
                // 写入token
                byte[] tokenBytes = BitConverter.GetBytes(token);
                Array.Copy(tokenBytes, 0, packet, offset, tokenBytes.Length);
                offset += tokenBytes.Length;
            }
            //写入数据
            Array.Copy(data, 0, packet, offset, data.Length);
            packet = BasePacket.PackOne(packet);
            return packet;
        }

        /// <summary>
        /// 读取一个数据包
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public override async Task<byte[]> ReadOne()
        {
            Token = 0;
            int offset = 0;
            byte[] data = await base.ReadOne();
            // 类型
            RequestType = (RequestEnum)data[0];
            // 是否Request
            IsRequest = data[1] == 1;
            // 是否有token
            bool hasToken = data[2] == 1;
            offset += 3;
            if (hasToken)
            {
                // 读取token
                Token = BitConverter.ToUInt32(data.Skip(2).Take(4).ToArray());
                offset += 4;
            }
            // 读取数据
            byte[] ret = new byte[data.Length - offset];
            Array.Copy(data, offset, ret, 0, ret.Length);
            return ret;
        }
    }
}
