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

        protected int CurStep { set; get; }
        /// <summary>
        ///     数据缓存
        /// </summary>
        protected List<byte> DataBuffer { set; get; } = new List<byte>();



        protected byte[] Header { set; get; }

        /// <summary>
        ///     命令类型
        /// </summary>
        public P2PCommandType CommandType { set; get; }

        protected int DataLength { set; get; }

        public byte[] Data { protected set; get; }

        public byte[] Footer { set; get; }

        protected int CurStreamReadLength { set; get; } = 0;


        public ReceivePacket()
        {
            Init();
        }

        public void Init()
        {
            CurStep = 0;
            DataBuffer.Clear();
            Header = new byte[0];
            DataLength = 0;
            Data = new byte[0];
            Footer = new byte[0];
            CommandType = P2PCommandType.UnKnown;
        }

        public virtual void Reset()
        {
            Init();
        }

        /// <summary>
        ///     解析数据包（header，type，length，data，footer）
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public virtual bool ParseData(ref byte[] data)
        {
            CurStreamReadLength = 0;
            //包头
            if (ParseHeader(data)
                //命令
                && ParseCommand(data)
                //数据长度
                && ParsePacketLength(data)
                //数据
                && ParseBody(data)
                //包尾
                && ParseFooter(data))
            {
                //成功读取一个完整包
                data = data.Skip(CurStreamReadLength).ToArray();
                return true;
            }
            return false;
        }

        protected virtual void ReadBytes(int count, byte[] data)
        {
            int dataCount = count - DataBuffer.Count;
            int readLength = (data.Length - CurStreamReadLength) >= dataCount ? dataCount : (data.Length - CurStreamReadLength);
            if (readLength == 0) return;
            if (readLength < 0) throw new OverflowException("数据包解析错误-逻辑错误");
            DataBuffer.AddRange(data.Skip(CurStreamReadLength).Take(readLength));
            CurStreamReadLength += readLength;
        }

        /// <summary>
        ///     解析头部，判断是否P2P应用数据包
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual bool ParseHeader(byte[] data)
        {
            bool ret = false;
            if ((CurStep & 0x1) == 0)
            {
                ReadBytes(2, data);
                if (DataBuffer.Count == 2)
                {
                    Header = DataBuffer.ToArray();
                    if (DataBuffer[0] == P2PGlobal.PacketHeader[0] && DataBuffer[1] == P2PGlobal.PacketHeader[1])
                    {
                        DataBuffer.Clear();
                        CurStep |= 0x1;
                        ret = true;
                    }
                    else
                    {
                        throw new InvalidDataException("非法的tcp数据包");
                    }
                }
            }
            else
            {
                ret = true;
            }
            return ret;
        }
        protected virtual bool ParseCommand(byte[] data)
        {
            bool ret = false;
            if ((CurStep & 0x10) == 0)
            {
                ReadBytes(2, data);
                if (DataBuffer.Count == 2)
                {
                    CommandType = (P2PCommandType)((DataBuffer[1] << 8) + DataBuffer[0]);
                    DataBuffer.Clear();
                    CurStep |= 0x10;
                    ret = true;
                }
            }
            else
            {
                ret = true;
            }
            return ret;
        }


        /// <summary>
        ///     读取数据包长度
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual bool ParsePacketLength(byte[] data)
        {
            bool ret = false;
            if ((CurStep & 0x100) == 0)
            {
                ReadBytes(2, data);
                if (DataBuffer.Count == 2)
                {
                    DataLength = BitConverter.ToInt16(DataBuffer.ToArray(), 0);
                    DataBuffer.Clear();
                    CurStep |= 0x100;
                    ret = true;
                }
            }
            else
            {
                ret = true;
            }
            return ret;
        }
        protected virtual bool ParseBody(byte[] data)
        {
            bool ret = false;
            if ((CurStep & 0x1000) == 0)
            {
                ReadBytes(DataLength, data);
                if (DataBuffer.Count == DataLength)
                {
                    Data = DataBuffer.ToArray();
                    DataBuffer.Clear();
                    CurStep |= 0x1000;
                    ret = true;
                }
            }
            else
            {
                ret = true;
            }
            return ret;
        }
        /// <summary>
        ///     读取末尾标识符
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual bool ParseFooter(byte[] data)
        {
            bool ret = false;
            if ((CurStep & 0x10000) == 0)
            {
                ReadBytes(1, data);
                if (DataBuffer.Count == 1)
                {
                    if (DataBuffer[0] == P2PGlobal.PacketFooter)
                    {
                        DataBuffer.Clear();
                        CurStep |= 0x10000;
                        ret = true;
                    }
                    else
                    {
                        throw new Exception("数据包读取错误：未找到包末尾！");
                    }
                }
            }
            else
            {
                ret = true;
            }
            return ret;
        }

        public byte[] GetBytes()
        {
            return Data;
        }
    }
}
