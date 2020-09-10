using P2PSocket.Core.Enums;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace P2PSocket.Core.CoreImpl
{

    public class Logger : ILogger
    {
        private TaskFactory m_taskFactory = new TaskFactory();
        private Task m_curTask = null;
        private object m_obj = new object();
        private ConcurrentQueue<LogInfo> m_logList = new ConcurrentQueue<LogInfo>();
        public Logger()
        {
        }

        public virtual void WriteLine(LogInfo log)
        {
            if (log.LogLevel == LogLevel.None) return;
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
                try
                {
                    IFileManager fileInst = EasyInject.Get<IFileManager>();
                    if (!fileInst.IsExist(IFileManager.Log))
                    {
                        fileInst.Create(IFileManager.Log);
                    }
                    fileInst.ForeachWrite(IFileManager.Log, BatchWrite);
                }
                catch
                {
                    isError = true;
                    Thread.Sleep(2000);
                }
            } while (isError);
            m_curTask = null;
        }
        protected virtual void BatchWrite(Action<string> writeOneFunc)
        {
            Thread.Sleep(500);
            do
            {
                if (!m_logList.IsEmpty && m_logList.TryDequeue(out LogInfo logInfo))
                {
                    writeOneFunc($"{logInfo.Time:HH:mm:ss:ffff} >> {logInfo.Msg}");
                }
            } while (m_logList.Count > 0);
        }
    }
}
