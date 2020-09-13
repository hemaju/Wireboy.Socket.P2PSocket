using P2PSocket.Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Core.Models
{
    public interface ILogger
    {
        void WriteLine(LogInfo log);
        void SetFilter(Func<LogInfo, bool> filter);
        event EventHandler<LogInfo> OnWriteLog;
    }
}
