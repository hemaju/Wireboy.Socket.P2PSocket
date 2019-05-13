using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace P2PSocket.Core.Utils
{
    public class ConsoleUtils
    {
        private static Task m_curConsoleTask = null;
        private static object m_consoleObj = new object();
        private static ConcurrentQueue<string> m_consoleLogList = new ConcurrentQueue<string>();
        private static TaskFactory m_taskFactory = new TaskFactory();

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
        public static void WriteLine(string log, object arg0 = null, object arg1 = null, object arg2 = null)
        {
            ConsoleUtils.WriteConsole(string.Format(log, arg0, arg1, arg2));
        }
    }
}
