using System;
using System.Collections.Generic;
using System.Text;

namespace Wireboy.Socket.P2PService.Models
{
    public class ApplicationConfig
    {
        [ConfigField("服务器通讯端口号")]
        public int ServerPort { get; set; } = 3488;

        [ConfigField("日志等级")]
        public LogLevel LogLevel { set; get; } = LogLevel.Info;
    }
}
