using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Core
{
    public static class P2PGlobal
    {
        /// <summary>
        ///     数据接收缓存区大小
        /// </summary>
        public const int P2PSocketBufferSize = 1470;
        /// <summary>
        ///     固定的包头数据
        /// </summary>
        public static readonly byte[] PacketHeader = { 86, 86 };
        /// <summary>
        ///     固定的包尾数据
        /// </summary>
        public const byte PacketFooter = 0x05;

    }

    public enum P2PCommandType : ushort
    {
        /// <summary>
        ///     未知的命令
        /// </summary>
        UnKnown = 0xffff,
        /// <summary>
        ///     心跳包
        /// </summary>
        Heart0x0052 = 0x0052,
        /// <summary>
        ///     身份认证
        /// </summary>
        Login0x0101 = 0x0101,
        /// <summary>
        ///     Token请求
        /// </summary>
        Login0x0102 = 0x0102,
        /// <summary>
        ///     客户端信息
        /// </summary>
        Login0x0103 = 0x0103,
        /// <summary>
        ///     匿名认证，通过mac获取ClientName
        /// </summary>
        Login0x0104 = 0x0104,
        /// <summary>
        ///     P2P端口映射请求
        /// </summary>
        P2P0x0201 = 0x0201,
        /// <summary>
        ///     P2P端口映射数据转发
        /// </summary>
        P2P0x0202 = 0x0202,
        /// <summary>
        ///     服务端口映射请求
        /// </summary>
        P2P0x0211 = 0x0211,
        /// <summary>
        ///     服务端口映射数据转发
        /// </summary>
        P2P0x0212 = 0x0212,
        /// <summary>
        ///     日志消息
        /// </summary>
        Msg0x0301 = 0x0301

    }

    public enum ResultCode : byte
    {
        成功 = 0x00,
        对方不在线 = 0x01,
        身份认证失败 = 0x02,
        无权限 = 0x03,
        其它错误 = 0xff
    }
}
