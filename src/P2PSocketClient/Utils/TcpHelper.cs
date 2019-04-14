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
        /// <summary>
        /// 数据长度
        /// </summary>
        int OnePackageDataLength { set; get; } = 0;
        /// <summary>
        /// 发生粘包的数据
        /// </summary>
        List<byte> Buffer { set; get; } = new List<byte>();
        /// <summary>
        /// 当前流位置对应的完整包位置
        /// </summary>
        int OnePackageCurIndex = 0;
        /// <summary>
        /// 数据长度缓存
        /// </summary>
        byte[] LengthBytes = new byte[] { 0, 0 };

        public ConcurrentQueue<byte[]> ReadPackages(byte[] bytes, int length = -1)
        {
            if (length == -1) length = bytes.Length;
            ConcurrentQueue<byte[]> ret = new ConcurrentQueue<byte[]>();
            int curIndex = 0;
            while (curIndex < length)
            {
                if (OnePackageCurIndex < 2)
                {
                    //说明是一个完整数据包的开头
                    if (bytes[curIndex] == TcpUtils.StartCode)
                    {
                        //说明数据包正常
                        curIndex += 1;
                        OnePackageCurIndex += 1;
                    }
                    else
                    {
                        OnePackageDataLength = 0;
                        Buffer.Clear();
                        OnePackageCurIndex = 0;
                        //说明数据包异常
                        throw new Exception("数据包异常！");
                    }
                }
                else if (OnePackageCurIndex < 4)
                {
                    //读取类别
                    Buffer.Add(bytes[curIndex]);
                    OnePackageCurIndex += 1;
                    curIndex += 1;
                }
                else if (OnePackageCurIndex < 6)
                {
                    //读取数据长度
                    LengthBytes[OnePackageCurIndex - 4] = bytes[curIndex];
                    if (OnePackageCurIndex == 5)
                    {
                        OnePackageDataLength = BitConverter.ToInt16(LengthBytes, 0);
                    }
                    OnePackageCurIndex += 1;
                    curIndex += 1;
                }
                else
                {
                    //读取数据

                    //获取包数据剩余读取长度
                    int curReadLength = OnePackageDataLength - OnePackageCurIndex + 6;
                    if (curReadLength > length - curIndex)
                    {
                        //说明接收的是部分数据
                        curReadLength = length - curIndex;
                        Buffer.AddRange(bytes.Skip(curIndex).Take(curReadLength));

                        OnePackageCurIndex += curReadLength;
                        curIndex += curReadLength;
                    }
                    else
                    {
                        //说明可完整接收数据
                        Buffer.AddRange(bytes.Skip(curIndex).Take(curReadLength));
                        ret.Enqueue(Buffer.ToArray());
                        //重置数据
                        Buffer = new List<byte>();
                        OnePackageCurIndex = 0;
                        curIndex += curReadLength;
                    }
                }
            }
            return ret;
        }
    }
}
