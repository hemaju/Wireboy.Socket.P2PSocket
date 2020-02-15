using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace P2PSocket.Core.Utils
{
    public enum LogLevel : int
    {
        /// <summary>
        ///     无日志模式
        /// </summary>
        None = 0,
        /// <summary>
        ///     致命消息
        /// </summary>
        Fatal = 1,
        /// <summary>
        ///     错误消息
        /// </summary>
        Error = 2,
        /// <summary>
        ///     警告消息
        /// </summary>
        Warning = 3,
        /// <summary>
        ///     一般消息
        /// </summary>
        Info = 4,
        /// <summary>
        ///     调试消息
        /// </summary>
        Debug = 5,
        /// <summary>
        ///     跟踪消息
        /// </summary>
        Trace = 6
    }
    public class Logger
    {
        public string FilePath
        {
            get
            {
                return $"{LogDirect}/{PreFix}{DateTime.Now.ToString("yyyy-MM-dd")}.log";
            }
        }

        public string LogDirect = "";
        public string PreFix = "";
        public LogLevel LogLevel { set; get; } = LogLevel.Debug;
        private TaskFactory m_taskFactory = new TaskFactory();
        private Task m_curTask = null;
        private object m_obj = new object();
        private ConcurrentQueue<LogInfo> m_logList = new ConcurrentQueue<LogInfo>();
        public Logger(string logDirectory, string preFix)
        {
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
            PreFix = preFix;
            LogDirect = logDirectory;
        }

        public virtual void WriteLine(LogLevel logLevel, string log)
        {
            if (logLevel == LogLevel.None) return;
            WriteLine(new LogInfo() { LogLevel = logLevel, Msg = log, Time = DateTime.Now });
        }
        public virtual void WriteLine(LogInfo log)
        {
            if (log.LogLevel == LogLevel.None) return;
            if (m_logList.Count > 100)
            {
                //如果堆栈中有100条消息未处理，则不再继续处理日志
                return;
            }
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

        protected virtual void DoWriteLine()
        {
            bool isError = false;
            do
            {
                isError = false;
                try
                {
                    if (!Directory.Exists(LogDirect))
                    {
                        Directory.CreateDirectory(LogDirect);
                    }
                    using (StreamWriter fileStream = new StreamWriter(FilePath, true))
                    {
                        Thread.Sleep(500);
                        do
                        {
                            if (!m_logList.IsEmpty && m_logList.TryDequeue(out LogInfo logInfo))
                            {
                                RecordLogEvent?.Invoke(fileStream, logInfo);
                            }
                        } while (m_logList.Count > 0);
                    }
                }
                catch
                {
                    isError = true;
                    Thread.Sleep(2000);
                }
            } while (isError);
            m_curTask = null;
        }

        public delegate void RecordLogHandler(StreamWriter ss, LogInfo logInfo);
        public event RecordLogHandler RecordLogEvent;
    }
    public class LogInfo
    {
        public LogLevel LogLevel { set; get; }
        public string Msg { set; get; }
        public DateTime Time { set; get; }
    }
}
