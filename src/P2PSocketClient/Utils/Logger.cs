using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Wireboy.Socket.P2PClient
{
    public static class Logger
    {
        private static TaskFactory _taskFactory = new TaskFactory();
        private static Task _curTask = null;
        private static object obj = new object();
        private static ConcurrentQueue<string> logList = new ConcurrentQueue<string>();

        public static void Write(string log)
        {
            log = string.Format("[{0:yyyy-MM-dd HH:mm:ss}]{1}", DateTime.Now, log);
            logList.Enqueue(log);
            if (_curTask == null)
            {
                lock (obj)
                {
                    if (_curTask == null)
                    {
                        _curTask = _taskFactory.StartNew(() => DoWrite());
                    }
                }
            }
        }
        public static void Write(string log, object arg0 = null, object arg1 = null, object arg2 = null)
        {
            Logger.Write(string.Format(log, arg0, arg1, arg2));
        }

        public static void Debug(string log, object arg0 = null, object arg1 = null, object arg2 = null)
        {
            if (ConfigServer.AppSettings.LogLevel == Models.LogLevel.调试模式)
                Logger.Write(string.Format(log, arg0, arg1,arg2));
        }

        private static void DoWrite()
        {
            try
            {
                string filePath =ConfigServer.LogFile;
                StreamWriter fileStream = new StreamWriter(filePath, true);
                do
                {
                    do
                    {
                        if (!logList.IsEmpty)
                        {
                            string str = "";
                            if (logList.TryDequeue(out str))
                            {
                                fileStream.WriteLine(str);
                            }
                        }
                    } while (logList.Count > 0);
                    Thread.Sleep(1000);
                } while (logList.Count > 0);
                fileStream.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0}", ex);
            }
            _curTask = null;
        }
    }
}
