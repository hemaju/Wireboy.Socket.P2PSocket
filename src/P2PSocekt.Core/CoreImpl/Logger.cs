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

    public class QueueThread
    {
        private ConcurrentQueue<Action> queue = new ConcurrentQueue<Action>();
        private Task m_curTask = null;
        private object m_obj = new object();
        public void Excute(Action func)
        {
            queue.Enqueue(func);
            if (m_curTask == null)
            {
                lock (m_obj)
                {
                    if (m_curTask == null)
                    {
                        m_curTask = Task.Factory.StartNew(() => DoExcute());
                    }
                }
            }
        }
        protected virtual void DoExcute()
        {
            Thread.Sleep(500);
            do
            {
                if (!queue.IsEmpty && queue.TryDequeue(out Action func))
                {
                    func();
                }
            } while (queue.Count > 0);
            m_curTask = null;
        }
    }

    public class QueueBatchHandler<T>
    {
        private ConcurrentQueue<T> queue = new ConcurrentQueue<T>();
        private Task m_curTask = null;
        private object m_obj = new object();
        public void Excute(T func)
        {
            queue.Enqueue(func);
            if (m_curTask == null)
            {
                lock (m_obj)
                {
                    if (m_curTask == null)
                    {
                        m_curTask = Task.Factory.StartNew(() => DoExcute());
                    }
                }
            }
        }
        protected virtual void DoExcute()
        {
            Thread.Sleep(500);
            do
            {
                if (!queue.IsEmpty && queue.TryDequeue(out T func))
                {
                }
            } while (queue.Count > 0);
            m_curTask = null;
        }
    }


    public class Logger : ILogger
    {
        Func<LogInfo, bool> filterAction = null;
        private TaskFactory m_taskFactory = new TaskFactory();
        private Task m_curTask = null;
        private object m_obj = new object();
        private ConcurrentQueue<LogInfo> m_logList = new ConcurrentQueue<LogInfo>();

        public event EventHandler<LogInfo> OnWriteLog;
        QueueThread pipeTask = new QueueThread();

        public Logger()
        {
        }

        public virtual void WriteLine(LogInfo log)
        {
            pipeTask.Excute(()=> {
                OnWriteLog?.Invoke(this, log);
            });
            if (filterAction != null && filterAction(log))
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
        public void SetFilter(Func<LogInfo, bool> filter)
        {
            filterAction = filter;
        }
    }
}
