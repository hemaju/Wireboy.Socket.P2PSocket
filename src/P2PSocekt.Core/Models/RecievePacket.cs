using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using P2PSocket.Core.Commands;

namespace P2PSocket.Core.Models
{
    public class ReceivePacket
    {
        /// <summary>
        ///     当前在解析数据包的第几步
        /// </summary>
        protected string Step { set; get; } = "ParseHeader";
        /// <summary>
        ///     在一个完整数据包中读取的位置
        /// </summary>
        protected int PacketDataIndex { set; get; } = 0;
        /// <summary>
        ///     当前流读取位置
        /// </summary>
        protected int CurStreamIndex { set; get; } = 0;
        /// <summary>
        ///     数据缓存
        /// </summary>
        protected byte[] DataBuffer { set; get; }
        /// <summary>
        ///     每一个步骤中，下一次读取的长度
        /// </summary>
        protected int NextLength { set; get; } = -1;
        /// <summary>
        ///     命令类型
        /// </summary>
        public P2PCommandType CommandType { set; get; }


        public ReceivePacket()
        {

        }

        /// <summary>
        ///     解析数据包（header，type，length，data，footer）
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public virtual bool ParseData(ref byte[] data)
        {
            CurStreamIndex = 0;
            if (Step == "ParseHeader" && ParseHeader(ref data))
            {
                Step = "ParseCommand";
            }
            if (Step == "ParseCommand" && ParseCommand(ref data))
            {
                Step = "ParsePacketLength";
            }
            if (Step == "ParsePacketLength" && ParsePacketLength(ref data))
            {
                Step = "ParseBody";
            }
            if (Step == "ParseBody" && ParseBody(ref data))
            {
                Step = "ParseFooter";
            }
            if (Step == "ParseFooter" && ParseFooter(ref data))
            {
                Step = "Finish";
            }
            if (Step == "Finish")
            {
                //说明读取一个完整包
                data = data.Skip(CurStreamIndex).ToArray();
                return true;
            }
            else
                return false;
        }

        /// <summary>
        ///     解析头部，判断是否P2P应用数据包
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual bool ParseHeader(ref byte[] data)
        {
            if (NextLength == -1)
            {
                NextLength = P2PGlobal.PacketHeader.Length;
            }
            for (; NextLength > 0 && CurStreamIndex < data.Length; CurStreamIndex++)
            {
                if (P2PGlobal.PacketHeader[2 - NextLength] != data[CurStreamIndex])
                {
                    throw new Exception("非法的tcp数据包");
                }
                NextLength--;
            }
            if (NextLength == 0)
            {
                NextLength = -1;
                return true;
            }
            else
                return false;
        }
        protected virtual bool ParseCommand(ref byte[] data)
        {
            if (NextLength == -1)
            {
                DataBuffer = new byte[2];
                NextLength = 2;
            }
            for (; NextLength > 0 && CurStreamIndex < data.Length; CurStreamIndex++)
            {
                DataBuffer[2-NextLength] = data[CurStreamIndex];
                NextLength--;
            }
            if (NextLength == 0)
            {
                NextLength = -1;
                CommandType = (P2PCommandType)((DataBuffer[1] << 8) + DataBuffer[0]);
                return true;
            }
            else
                return false;
        }
        /// <summary>
        ///     读取数据包长度
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual bool ParsePacketLength(ref byte[] data)
        {
            if (NextLength == -1)
            {
                //包长度使用short，所以需要2字节存储
                DataBuffer = new byte[2];
                NextLength = 2;
            }

            for (; NextLength > 0 && CurStreamIndex < data.Length; CurStreamIndex++)
            {
                //长度数据放入缓存
                DataBuffer[2 - NextLength] = data[CurStreamIndex];
                NextLength--;
            }
            if (NextLength == 0)
            {
                int packetLength = BitConverter.ToInt16(DataBuffer, 0);
                DataBuffer = new byte[packetLength];
                NextLength = -1;
                return true;
            }
            else
                return false;
        }
        protected virtual bool ParseBody(ref byte[] data)
        {
            if (NextLength == -1)
            {
                //在第3步时已设置了新的缓存
                NextLength = DataBuffer.Length;
            }
            int readLength = NextLength;
            //如果超长，需要重新设置读取长度
            if (readLength > data.Length - CurStreamIndex)
                readLength = data.Length - CurStreamIndex;
            if (readLength > 0)
            {
                data.Skip(CurStreamIndex).Take(readLength).ToArray().CopyTo(DataBuffer, PacketDataIndex);
                //计算剩余数据长度
                NextLength -= readLength;
                //计算当前流位置
                CurStreamIndex += readLength;
                //计算数据包读取到的位置
                PacketDataIndex += readLength;
            }

            if (NextLength == 0)
            {
                NextLength = -1;
                return true;
            }
            else
                return false;
        }
        /// <summary>
        ///     读取末尾标识符
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual bool ParseFooter(ref byte[] data)
        {
            if (NextLength == -1)
            {
                NextLength = 1;
            }
            if (CurStreamIndex < data.Length)
            {
                NextLength--;
                if (data[CurStreamIndex] == P2PGlobal.PacketFooter)
                {
                    //末尾1字节
                    CurStreamIndex++;
                    return true;
                }
                else
                    throw new Exception("数据包读取错误：未找到包末尾！");
            }
            return false;
        }

        public byte[] GetBytes()
        {
            return DataBuffer;
        }

        public virtual void Reset()
        {
            Step = "ParseHeader";
            PacketDataIndex = 0;
            CurStreamIndex = 0;
            DataBuffer = null;
            NextLength = -1;
        }
    }
}
