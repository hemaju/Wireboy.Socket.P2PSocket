using System;
using System.Collections.Generic;
using System.Text;

namespace Wireboy.Socket.P2PService.Models
{
    public enum MsgType
    {
        不封包 = -1,
        心跳包 = 0,
        身份验证 = 1,
        本地服务名 = 2,
        远程服务名 = 3,
        转发FromClient = 4,
        转发FromHome = 5,
        断开FromClient = 6,
        断开FromHome = 7,
        测试服务器 = 8,
        测试客户端 = 9,
        Http服务 = 10,
        无类型 = 11
    }
    public enum HttpMsgType
    {
        None = 0,
        Http服务名 = 1,
        Http数据 = 2
    }
    public enum LogLevel
    {
        运行模式 = 0,
        调试模式 = 1
    }
}
