using P2PSocket.Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Client.Utils
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

        public static Logger Instance { get; } = new Logger($"{Global.RuntimePath}P2PSocket/Logs","Client_");
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
        public static void WriteLine(LogInfo log)
        {
            ConsoleUtils.Show(log.LogLevel, log.Msg);
            Instance.WriteLine(log);
        }
    }
}
