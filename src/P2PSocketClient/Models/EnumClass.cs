using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wireboy.Socket.P2PClient.Models
{
    public enum MsgType
    {
        不封包 = -1,
        心跳包 = 0,
        身份验证 = 1,
        本地服务名 = 2,
        远程服务名 = 3,
        数据转发 = 4,
        连接断开 = 5,
        测试服务器 = 6,
        测试客户端 = 7
    }
}
