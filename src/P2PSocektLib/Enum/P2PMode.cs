using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PSocektLib.Enum
{
    public enum P2PMode
    {
        服务器中转 = 0,  
        Tcp端口复用 = 1, 
        Tcp端口预测 = 2, 
        Udp端口复用 = 3, 
        IP直连 = 4,
    }
}
