using P2PSocket.Core.Enums;
using P2PSocket.Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Server.Utils
{
    public static class ConsoleUtils
    {
        //多线程锁
        private static object wlock = new object();
        //日志实例句柄
        private static AppCenter _instance = null;
        private static AppCenter appCenter
        {
            get
            {
                if (_instance == null)
                {
                    lock (wlock)
                    {
                        if (_instance == null)
                            _instance = EasyInject.Get<AppCenter>();
                    }
                }
                return _instance;
            }
        }
        static ConsoleCore Instance { get; } = new ConsoleCore();
        public static void Show(LogLevel logLevel, string log)
        {
            if (appCenter.Config.LogLevel >= logLevel)
                Instance.WriteLine($"[{DateTime.Now:HH:mm:ss}] server# {log}");
        }
    }
}
