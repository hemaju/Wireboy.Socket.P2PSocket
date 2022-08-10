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
        public PipePacket(INetworkConnect conn):base(conn)
        {
        }

        public static byte[] PackOne(byte[] data, RequestEnum cmdType)
        {
            byte[] packet = new byte[data.Length + 1];
            packet[0] = (byte)cmdType;
            data.CopyTo(packet, 1);
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
            byte[] data = await base.ReadOne();
            RequestType = (RequestEnum)data[0];
            byte[] ret = new byte[data.Length - 1];
            data.CopyTo(ret, 0);
            Array.Copy(data, 1, ret, 0, ret.Length);
            return ret;
        }
    }
}
