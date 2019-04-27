using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wireboy.Socket.P2PClient.Models;

namespace Wireboy.Socket.P2PClient
{
    public static class Logger
    {
        private static TaskFactory m_taskFactory = new TaskFactory();
        private static Task m_curTask = null;
        private static object m_obj = new object();
        private static ConcurrentQueue<string> m_logList = new ConcurrentQueue<string>();


        private static Task m_curConsoleTask = null;
        private static object m_consoleObj = new object();
        private static ConcurrentQueue<string> m_consoleLogList = new ConcurrentQueue<string>();

        private static void WriteLine(string log)
        {
            log = string.Format("{0:MM-dd HH:mm:ss} {1}", DateTime.Now, log);
            m_logList.Enqueue(log);
            if (m_curTask == null)
            {
                lock (m_obj)
                {
                    if (m_curTask == null)
                    {
                        m_curTask = m_taskFactory.StartNew(() => DoWriteLine());
                    }
                }
            }
            WriteConsole(log);
        }
        private static void WriteLine(string log, object arg0 = null, object arg1 = null, object arg2 = null)
        {
            Logger.WriteLine(string.Format(log, arg0, arg1, arg2));
        }

        private static void DoWriteLine()
        {
            try
            {
                string filePath = ConfigServer.LogFile;
                StreamWriter fileStream = new StreamWriter(filePath, true);
                do
                {
                    do
                    {
                        if (!m_logList.IsEmpty)
                        {
                            string str = "";
                            if (m_logList.TryDequeue(out str))
                            {
                                fileStream.WriteLine(str);
                            }
                        }
                    } while (m_logList.Count > 0);
                    Thread.Sleep(1000);
                } while (m_logList.Count > 0);
                fileStream.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0}", ex);
            }
            m_curTask = null;
        }
        private static void WriteConsole(string log)
        {
            m_consoleLogList.Enqueue(log);
            if (m_curConsoleTask == null)
            {
                lock (m_consoleObj)
                {
                    if (m_curConsoleTask == null)
                    {
                        m_curConsoleTask = m_taskFactory.StartNew(() => DoWriteConsole());
                    }
                }
            }
        }

        private static void DoWriteConsole()
        {
            do
            {
                do
                {
                    if (!m_consoleLogList.IsEmpty)
                    {
                        string str = "";
                        if (m_consoleLogList.TryDequeue(out str))
                        {
                            System.Console.WriteLine(str);
                        }
                    }
                } while (m_consoleLogList.Count > 0);
                Thread.Sleep(200);
            } while (m_consoleLogList.Count > 0);
            m_curConsoleTask = null;
        }

        public static class Error
        {
            public static void WriteLine(string log, object arg0 = null, object arg1 = null, object arg2 = null)
            {
                if (ConfigServer.AppSettings.LogLevel >= LogLevel.Error)
                    Logger.WriteLine(string.Format(log, arg0, arg1, arg2));
            }
        }
        public static class Info
        {
            public static void WriteLine(string log, object arg0 = null, object arg1 = null, object arg2 = null)
            {
                if (ConfigServer.AppSettings.LogLevel >= LogLevel.Info)
                    Logger.WriteLine(string.Format(log, arg0, arg1, arg2));
                else
                    Logger.Console.WriteLine(string.Format(log, arg0, arg1, arg2));
            }
        }
        public static class Debug
        {
            public static void WriteLine(string log, object arg0 = null, object arg1 = null, object arg2 = null)
            {
                if (ConfigServer.AppSettings.LogLevel >= LogLevel.Debug)
                    Logger.WriteLine(string.Format(log, arg0, arg1, arg2));
            }
        }
        public static class Trace
        {
            public static void WriteLine(string log, object arg0 = null, object arg1 = null, object arg2 = null)
            {
                if (ConfigServer.AppSettings.LogLevel >= LogLevel.Trace)
                    Logger.WriteLine(string.Format(log, arg0, arg1, arg2));
            }
        }
        public static class Console
        {
            public static void WriteLine(string log, object arg0 = null, object arg1 = null, object arg2 = null)
            {
                Logger.WriteConsole(string.Format(log, arg0, arg1, arg2));
            }
        }
    }
}