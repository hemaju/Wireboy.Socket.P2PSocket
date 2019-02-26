using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wireboy.Socket.P2PHome.Models
{
    public enum MsgType
    {
        不封包 = -1,
        心跳包 = 0,
        身份验证 = 1,
        主控服务名 = 2,
        被控服务名 = 3,
        数据转发 = 4,
        连接断开 = 5
    }
}
