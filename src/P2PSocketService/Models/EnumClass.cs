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
        连接断开 = 6,
        测试服务器 = 7,
        测试客户端 = 8
    }
    public enum LogLevel
    {
        运行模式 = 0,
        调试模式 = 1
    }
}
