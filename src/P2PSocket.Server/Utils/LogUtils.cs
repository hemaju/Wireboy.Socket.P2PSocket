using P2PSocket.Core.CoreImpl;
using P2PSocket.Core.Enums;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using P2PSocket.Server.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Server
{
    public static class LogUtils
    {

        //多线程锁
        private static object wlock = new object();
        //日志实例句柄
        private static ILogger _instance = null;
        private static ILogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (wlock)
                    {
                        if (_instance == null)
                        {
                            _instance = EasyInject.Get<ILogger>();
                            AppCenter appCenter = EasyInject.Get<AppCenter>();
                            _instance.SetFilter(logInfo =>
                            {
                                return appCenter.Config.LogLevel >= logInfo.LogLevel;
                            });
                        }
                    }
                }
                return _instance;
            }
        }
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
            Instance.WriteLine(new LogInfo() { LogLevel = logLevel, Msg = log });
        }
        public static void WriteLine(LogInfo log, bool forceWriteConsole = true)
        {
            ConsoleUtils.Show(forceWriteConsole ? log.LogLevel : LogLevel.None, log.Msg);
            Instance.WriteLine(log);
        }
    }
}
