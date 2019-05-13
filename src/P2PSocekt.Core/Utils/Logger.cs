using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace P2PSocket.Core.Utils
{
    public enum LogLevel
    {
        /// <summary>
        ///     无日志模式
        /// </summary>
        None = 0,
        /// <summary>
        ///     错误模式
        /// </summary>
        Error = 1,
        /// <summary>
        ///     一般模式
        /// </summary>
        Info = 2,
        /// <summary>
        ///     警告模式
        /// </summary>
        Warning = 3,
        /// <summary>
        ///     调试模式
        /// </summary>
        Debug = 4
    }
    public class Logger
    {
        public  string LogFile { set; get; }
        public  LogLevel LogLevel { set; get; } = LogLevel.None;
        private  TaskFactory m_taskFactory = new TaskFactory();
        private  Task m_curTask = null;
        private  object m_obj = new object();
        private  ConcurrentQueue<string> m_logList = new ConcurrentQueue<string>();
        public Logger(string fileName = "logs.log")
        {
            LogFile = fileName;
        }

        protected virtual void WriteLine(string log)
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
        }
        public virtual void WriteLine(LogLevel logLevel, string log, object arg0 = null, object arg1 = null, object arg2 = null)
        {
            if (LogLevel >= logLevel)
                WriteLine(string.Format(log, arg0, arg1, arg2));
        }

        protected virtual void DoWriteLine()
        {
            try
            {
                StreamWriter fileStream = new StreamWriter(LogFile, true);
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
            }
            m_curTask = null;
        }
    }
}
