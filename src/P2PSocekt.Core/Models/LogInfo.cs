using P2PSocket.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Core.Models
{
    public class LogInfo
    {
        public LogLevel LogLevel { set; get; }
        public string Msg { set; get; }
        public DateTime Time { set; get; } = DateTime.Now;
    }
}
