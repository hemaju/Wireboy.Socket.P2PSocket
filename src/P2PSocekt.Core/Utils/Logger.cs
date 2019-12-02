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
        public string FilePath
        {
            get
            {
                return $"{LogDirect}/{PreFix}{DateTime.Now.ToString("yyyy-MM-dd")}.log";
            }
        }

        public string LogDirect = "";
        public string PreFix = "";
        public LogLevel LogLevel { set; get; } = LogLevel.Error;
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
            m_logList.Enqueue(new LogInfo() { LogLevel = logLevel, Msg = log, Time = DateTime.Now });
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
                    using (StreamWriter fileStream = new StreamWriter(FilePath, true))
                    {
                        Thread.Sleep(500);
                        do
                        {
                            if (!m_logList.IsEmpty)
                            {
                                LogInfo logInfo = null;
                                if (m_logList.TryDequeue(out logInfo))
                                {
                                    try
                                    {
                                        RecordLogEvent?.Invoke(fileStream, logInfo);
                                    }
                                    catch(Exception ex)
                                    {
                                        fileStream.WriteLine($"文件写入失败：{Environment.NewLine}{ex.Message}");
                                    }
                                }
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
