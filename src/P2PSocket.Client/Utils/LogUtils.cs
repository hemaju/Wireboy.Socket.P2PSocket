using P2PSocket.Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using static P2PSocket.Core.Utils.Logger;

namespace P2PSocket.Client.Utils
{
    public static class LogUtils
    {
        public static void InitConfig()
        {
            Instance.RecordLogEvent += ClientRecordLogHandler;
        }

        public static RecordLogHandler ClientRecordLogHandler = Instance_RecordLogEvent;

        private static void Instance_RecordLogEvent(System.IO.StreamWriter ss, LogInfo logInfo)
        {
            if (Instance.LogLevel >= logInfo.LogLevel)
                ss.WriteLine($"{logInfo.Time.ToString("[HH:mm:ss.ffff]")}{logInfo.Msg}");
        }

        public static Logger Instance { get; } = new Logger($"{AppCenter.Instance.RuntimePath}P2PSocket/Logs", "Client_");

        public static void Debug(string log, bool forceWriteConsole = true)
        {
            WriteLine(LogLevel.Debug, log, forceWriteConsole);
        }
        public static void Error(string log, bool forceWriteConsole = true)
        {
            WriteLine(LogLevel.Error, log, forceWriteConsole);
        }
        public static void Info(string log, bool forceWriteConsole = true)
        {
            WriteLine(LogLevel.Info, log, forceWriteConsole);
        }
        public static void Warning(string log, bool forceWriteConsole = true)
        {
            WriteLine(LogLevel.Warning, log, forceWriteConsole);
        }
        public static void Trace(string log, bool forceWriteConsole = true)
        {
            WriteLine(LogLevel.Trace, log, forceWriteConsole);
        }
        public static void Fatal(string log, bool forceWriteConsole = true)
        {
            WriteLine(LogLevel.Fatal, log, forceWriteConsole);
        }
        public static void WriteLine(LogLevel logLevel, string log, bool forceWriteConsole = true)
        {
            ConsoleUtils.Show(forceWriteConsole ? logLevel : LogLevel.None, log);
            Instance.WriteLine(logLevel, log);
        }
        public static void WriteLine(LogInfo log, bool forceWriteConsole = true)
        {
            ConsoleUtils.Show(forceWriteConsole ? log.LogLevel : LogLevel.None, log.Msg);
            Instance.WriteLine(log);
        }
    }
}
