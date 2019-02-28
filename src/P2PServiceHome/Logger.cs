using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Wireboy.Socket.P2PHome
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
        public static void Write(string log, object arg0)
        {
            Logger.Write(string.Format(log, arg0));
        }
        public static void Write(string log, object arg0, object arg1)
        {
            Logger.Write(string.Format(log, arg0, arg1));
        }

        private static void DoWrite()
        {
            try
            {
                string filePath = @"P2PHomeLog.log";
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
            catch
            {

            }
            _curTask = null;
        }
    }
}
