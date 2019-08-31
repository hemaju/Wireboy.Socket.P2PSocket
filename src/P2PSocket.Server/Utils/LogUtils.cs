using P2PSocket.Core.Utils;
using P2PSocket.Server.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Server
{
    public static class LogUtils
    {
        public static void InitConfig()
        {
            Instance.RecordLogEvent += Instance_RecordLogEvent;
        }

        private static void Instance_RecordLogEvent(System.IO.StreamWriter ss, LogInfo logInfo)
        {
            if (Instance.LogLevel >= logInfo.LogLevel)
                ss.WriteLine($"{logInfo.Time.ToString("[HH:mm:ss]")}{logInfo.Msg}");
        }

        public static Logger Instance { get; } = new Logger($"{Global.RuntimePath}P2PSocket/Logs","Server_");
        public static void Show(string log)
        {
            ConsoleUtils.Show(LogLevel.None, log);
            Instance.WriteLine(LogLevel.Info, log);
        }
        public static void Debug(string log)
        {
            WriteLine(LogLevel.Debug, log);
        }
        public static void Error(string log)
        {
            WriteLine(LogLevel.Error, log);
        }
        public static void Info(string log)
        {
            WriteLine(LogLevel.Info, log);
        }
        public static void Warning(string log)
        {
            WriteLine(LogLevel.Warning, log);
        }
        public static void WriteLine(LogLevel logLevel, string log)
        {
            ConsoleUtils.Show(logLevel, log);
            Instance.WriteLine(logLevel, log);
        }
    }
}
