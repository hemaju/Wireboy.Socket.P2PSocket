using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wireboy.Socket.P2PClient
{
    public class TcpHelper
    {
        public enum BufferTypeEnum
        {
            Length,
            Data
        }
        /// <summary>
        /// 数据长度
        /// </summary>
        public int PackageLength { set; get; } = 2;
        /// <summary>
        /// 发生粘包的数据
        /// </summary>
        public List<byte> Buffer { set; get; } = new List<byte>();

        public BufferTypeEnum BufferType { set; get; } = BufferTypeEnum.Length;

        public ConcurrentQueue<byte[]> RecieveTcp(byte[] bytes, int length = -1)
        {
            if (length == -1) length = bytes.Length;
            ConcurrentQueue<byte[]> ret = new ConcurrentQueue<byte[]>();
            int curIndex = 0;
            while (curIndex < length)
            {
                bool isReadComplate = true;
                int readLength = PackageLength;
                if (curIndex + readLength > length)
                {
                    isReadComplate = false;
                    readLength = length - curIndex;
                    PackageLength -= readLength;
                }
                Buffer.AddRange(bytes.Skip(curIndex).Take(readLength));
                if (isReadComplate)
                {
                    if (BufferType == BufferTypeEnum.Length)
                    {
                        PackageLength = BitConverter.ToInt16(Buffer.ToArray(),0);
                        BufferType = BufferTypeEnum.Data;
                    }
                    else
                    {
                        PackageLength = 2;
                        BufferType = BufferTypeEnum.Length;
                        ret.Enqueue(Buffer.ToArray());
                    }
                    Buffer.Clear();
                }
                curIndex += readLength;
            }
            return ret;
        }
    }
}
