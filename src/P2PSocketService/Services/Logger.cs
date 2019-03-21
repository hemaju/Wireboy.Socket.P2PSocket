/*
 * 日志服务 
 * 记录日志，支持并发，是线程安全的
 * 
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wireboy.Socket.P2PService.Models;

namespace Wireboy.Socket.P2PService
{
    public static class Logger
    {
        /// <summary>
        /// 线程工厂
        /// </summary>
        private static TaskFactory _taskFactory = new TaskFactory();
        /// <summary>
        /// 当前正在写日志的任务
        /// </summary>
        private static Task _curTask = null;
        /// <summary>
        /// 写日志任务锁
        /// </summary>
        private static object _lockObj = new object();
        /// <summary>
        /// 日志队列
        /// </summary>
        private static ConcurrentQueue<string> _logList = new ConcurrentQueue<string>();
        /// <summary>
        /// 日志格式
        /// </summary>
        private const string _logFormate = "[{0:yyyy-MM-dd HH:mm:ss}]{1}";
        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="log">内容</param>

        public static void Write(string log)
        {
            log = string.Format(_logFormate, DateTime.Now, log);
            if (_curTask == null)
            {
                lock (_lockObj)
                {
                    if (_curTask == null)
                    {
                        _curTask = _taskFactory.StartNew(() => DoWrite());
                    }
                    else
                    {
                        _logList.Enqueue(log);
                    }
                }
            }
            else
            {
                _logList.Enqueue(log);
            }
        }
        /// <summary>
        /// 记录日志（例如：Write("{0}_{1}","内容","参数"）
        /// </summary>
        /// <param name="log">内容</param>
        /// <param name="arg0">格式化参数</param>
        public static void Write(string log, object arg0)
        {
            Logger.Write(string.Format(log, arg0));
        }
        /// <summary>
        /// 记录日志（例如：Write("{0}_{1}_{2}","内容","参数1","参数2"）
        /// </summary>
        /// <param name="log">内容</param>
        /// <param name="arg0">格式化参数1</param>
        /// <param name="arg1">格式化参数2</param>
        public static void Write(string log, object arg0, object arg1)
        {
            Logger.Write(string.Format(log, arg0, arg1));
        }

        /// <summary>
        /// 写日志文件
        /// </summary>
        private static void DoWrite()
        {
            try
            {

                string filePath = ApplicationConfig.LogFile;
                StreamWriter fileStream = new StreamWriter(filePath, true);
                try
                {
                    string str = "";
                    do
                    {
                        while (_logList.TryDequeue(out str))
                        {
                            fileStream.WriteLine(str);
                        }
                        Thread.Sleep(1000);
                    } while (_logList.TryDequeue(out str));
                }
                catch
                {

                }
                fileStream.Close();
            }
            catch
            {
            }
            _curTask = null;
        }
    }
}
