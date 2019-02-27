using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wireboy.Socket.P2PClient.Models
{
    public class ApplicationConfig
    {
        /// <summary>
        /// 服务器通讯端口
        /// </summary>
        public int ServerPort { get; set; } = 3488;
        /// <summary>
        /// 服务器ip地址
        /// </summary>
        public string ServerIp { set; get; } = "127.0.0.1";
        /// <summary> 
        /// 主动连接的其它服务端口
        /// </summary>
        public int OtherServerPort { get; set; } = 3389;
        /// <summary>
        /// 服务监听端口
        /// </summary>
        public int LocalListenPort { get; set; } = 3588;
        /// <summary>
        /// 服务名称
        /// </summary>
        public string ServerName { set; get; } = "";
    }
}
