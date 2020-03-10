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

        public static Logger Instance { get; } = new Logger($"{AppCenter.Instance.RuntimePath}P2PSocket/Logs", "Server_");

        public static void Debug(string log, bool autoConsole = true)
        {
            WriteLine(LogLevel.Debug, log, autoConsole);
        }
        public static void Error(string log, bool autoConsole = true)
        {
            WriteLine(LogLevel.Error, log, autoConsole);
        }
        public static void Info(string log, bool autoConsole = true)
        {
            WriteLine(LogLevel.Info, log, autoConsole);
        }
        public static void Warning(string log, bool autoConsole = true)
        {
            WriteLine(LogLevel.Warning, log, autoConsole);
        }
        public static void Trace(string log, bool autoConsole = true)
        {
            WriteLine(LogLevel.Trace, log, autoConsole);
        }
        public static void Fatal(string log, bool autoConsole = true)
        {
            WriteLine(LogLevel.Fatal, log, autoConsole);
        }
        public static void WriteLine(LogLevel logLevel, string log, bool autoConsole = true)
        {
            ConsoleUtils.Show(autoConsole ? logLevel : LogLevel.None, log);
            Instance.WriteLine(logLevel, log);
        }
        public static void WriteLine(LogInfo log, bool autoConsole = true)
        {
            ConsoleUtils.Show(autoConsole ? log.LogLevel : LogLevel.None, log.Msg);
            Instance.WriteLine(log);
        }
    }
}
