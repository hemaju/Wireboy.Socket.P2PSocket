using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Wireboy.Socket.P2PClient.Models
{
    public delegate void RecievedTcpDataHander(byte[] data, TcpResult tcpResult);
    public enum StickyType
    {
        None = 0,
        Length = 1,
        Data = 2
    }
    public class TcpResult
    {
        /// <summary>
        /// 数据缓存
        /// </summary>
        public byte[] Readbuffer = new byte[1024];
        /// <summary>
        /// 数据流
        /// </summary>
        public NetworkStream ReadStream = null;
        /// <summary>
        /// 当前Tcp连接
        /// </summary>
        public TcpClient ReadTcp = null;
        /// <summary>
        /// 包总长度
        /// </summary>
        public int DataLength = 0;
        /// <summary>
        /// 已接收的长度
        /// </summary>
        public int RecievedLength = 0;
        /// <summary>
        /// 粘包类型
        /// </summary>
        public StickyType StickyMode = StickyType.None;
        /// <summary>
        /// 粘包时，已接收的粘包数据
        /// </summary>
        public byte[] StickeyPackageData = null;
        private RecievedTcpDataHander RecievedTcpDataCallBack = null;


        public TcpResult(NetworkStream readStream, TcpClient readTcp, RecievedTcpDataHander recievedTcpDataCallBack)
        {
            ReadStream = readStream;
            ReadTcp = readTcp;
            RecievedTcpDataCallBack = recievedTcpDataCallBack;
        }

        /// <summary>
        /// 清空数据缓存区
        /// </summary>
        public void ResetReadBuffer()
        {
            Readbuffer = new byte[1024];
        }
        /// <summary>
        /// 重置粘包缓存数据
        /// </summary>
        public void ResetStickyState()
        {
            DataLength = 0;
            RecievedLength = 0;
            StickyMode = StickyType.None;
            StickeyPackageData = null;
        }

        public byte[] MergeData(byte[] bytes1, byte[] bytes2)
        {
            byte[] retBytes = new byte[bytes1.Length + bytes2.Length];
            bytes1.CopyTo(retBytes, 0);
            bytes2.CopyTo(retBytes, bytes1.Length);
            return retBytes;
        }

        /// <summary>
        /// 读取一个数据包
        /// </summary>
        /// <param name="streamLength">数据流总长度</param>
        /// <param name="curIndex">当前读取位置</param>
        public void ReadOnePackageData(int streamLength, ref int curIndex)
        {
            if (this.StickyMode != StickyType.None)
            {
                //粘包了
                if (this.StickyMode == StickyType.Length)
                {
                    //处理长度粘包
                    byte[] countBytes;
                    this.DataLength = 2;
                    if (ReadData(streamLength, ref curIndex, this.DataLength - this.RecievedLength, out countBytes))
                    {
                        byte[] curBytes = MergeData(this.StickeyPackageData, countBytes);
                        this.DataLength = BitConverter.ToInt16(curBytes,0);
                        SetStickyProp(null, StickyType.None, 0);
                        //读取包数据
                        byte[] dataBytes;
                        if (ReadData(streamLength, ref curIndex, this.DataLength, out dataBytes))
                        {
                            SetStickyProp(null, StickyType.None, 0);
                            DoRecievedPackage(dataBytes);
                        }
                        else
                        {
                            //产生数据粘包
                            SetStickyProp(dataBytes, StickyType.Data, dataBytes.Length);
                        }
                    }
                    else
                    {
                        //长度仍然粘包
                        byte[] curBytes = MergeData(this.StickeyPackageData, countBytes);
                        SetStickyProp(curBytes, StickyType.Length, curBytes.Length);
                    }
                }
                else if (this.StickyMode == StickyType.Data)
                {
                    //处理数据粘包
                    byte[] dataBytes;
                    if (ReadData(streamLength, ref curIndex, this.DataLength - this.RecievedLength, out dataBytes))
                    {
                        //读取完毕
                        dataBytes = MergeData(this.StickeyPackageData, dataBytes);
                        SetStickyProp(null, StickyType.None, 0);
                        DoRecievedPackage(dataBytes);
                    }
                    else
                    {
                        //数据仍然粘包
                        dataBytes = MergeData(this.StickeyPackageData, dataBytes);
                        //产生粘包
                        SetStickyProp(dataBytes, StickyType.Data, dataBytes.Length);
                    }
                }
            }
            else
            {
                byte[] countBytes;
                this.DataLength = 2;
                if (ReadData(streamLength, ref curIndex, this.DataLength, out countBytes))
                {
                    this.DataLength = BitConverter.ToInt16(countBytes,0);
                    byte[] dataBytes;
                    if (ReadData(streamLength, ref curIndex, this.DataLength, out dataBytes))
                    {
                        SetStickyProp(null, StickyType.None, 0);
                        DoRecievedPackage(dataBytes);
                    }
                    else
                    {
                        //产生数据粘包
                        SetStickyProp(dataBytes, StickyType.Data, dataBytes.Length);
                    }
                }
                else
                {
                    //产生长度粘包
                    SetStickyProp(countBytes, StickyType.Length, countBytes.Length);
                }
            }
        }

        public bool ReadData(int streamLength, ref int curIndex, int dataLength, out byte[] readBytes)
        {
            //需要判断有没有超长（粘包）
            int readLength = streamLength >= (curIndex + dataLength) ? dataLength : (streamLength - curIndex);
            //读取数据
            readBytes = Readbuffer.Skip(curIndex).Take(readLength).ToArray();
            //设置读取流位置
            curIndex += readBytes.Length;
            return readLength == dataLength;
        }

        public void SetStickyProp(byte[] bytes, StickyType stickyType, int recievedLength)
        {
            this.StickeyPackageData = bytes;
            this.StickyMode = stickyType;
            this.RecievedLength = recievedLength;
        }

        public void DoRecievedPackage(byte[] bytes)
        {
            if (bytes.Length == 0) return;
            try
            {
                Logger.Debug("处理数据包，长度：{0} 来自：{1}",bytes.Length,ReadTcp.Client.RemoteEndPoint);
                RecievedTcpDataCallBack?.Invoke(bytes, this);
            }
            catch (Exception ex)
            {
                Logger.Write("处理数据包错误：{0}", ex);
            }
        }
    }
}
