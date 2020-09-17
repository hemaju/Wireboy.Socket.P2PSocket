using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace P2PSocket.Core.Utils
{
    public class ConsoleCore
    {
        private Task m_curConsoleTask = null;
        private object m_consoleObj = new object();
        private ConcurrentQueue<string> m_consoleLogList = new ConcurrentQueue<string>();
        private static TaskFactory m_taskFactory = new TaskFactory();
        private bool isConsoleAvailable = true;

        private void WriteConsole(string log)
        {
            if (isConsoleAvailable && m_consoleLogList.Count <= 10000)
            {
                m_consoleLogList.Enqueue(log);
                if (m_curConsoleTask == null)
                {
                    lock (m_consoleObj)
                    {
                        if (m_curConsoleTask == null || m_curConsoleTask.IsCompleted)
                        {
                            m_curConsoleTask = m_taskFactory.StartNew(() => DoWriteConsole());
                        }
                    }
                }
            }
        }
        private void DoWriteConsole()
        {
            do
            {
                do
                {
                    if (!m_consoleLogList.IsEmpty)
                    {
                        string str;
                        if (m_consoleLogList.TryDequeue(out str))
                        {
                            try
                            {
                                System.Console.WriteLine(str);
                            }
                            catch
                            {
                                isConsoleAvailable = false;
                                m_consoleLogList = new ConcurrentQueue<string>();
                            }
                        }
                    }
                } while (m_consoleLogList.Count > 0);
                Thread.Sleep(200);
            } while (m_consoleLogList.Count > 0);
            m_curConsoleTask = null;
        }
        public void WriteLine(string log, object arg0 = null, object arg1 = null, object arg2 = null)
        {
            WriteConsole(string.Format(log, arg0, arg1, arg2));
        }
    }
}
