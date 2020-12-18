using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace P2PSocket.Core.Utils
{
    public class TimerUtils
    {
        static TimerUtils _Instance = new TimerUtils();
        public static TimerUtils Instance
        {
            get => _Instance;
        }
        List<TaskInfo> TaskInfos { set; get; } = new List<TaskInfo>();
        object timerMonitor { set; get; } = new object();
        Task TimerTask { set; get; }
        object excuteBlog { set; get; } = new object();
        object taskBlock { set; get; } = new object();

        /// <summary>
        /// 添加定时执行的任务
        /// </summary>
        /// <param name="func">运行的方法</param>
        /// <param name="delayTime">延迟时间（毫秒）</param>
        public void AddJob(Action func, int delayTime)
        {
            DateTime excuteTime = DateTime.Now.AddMilliseconds(delayTime);
            AddJob(func, excuteTime);
        }
        /// <summary>
        /// 添加定时执行的任务
        /// </summary>
        /// <param name="func">运行的方法</param>
        /// <param name="delayTime">时间</param>
        public void AddJob(Action func, DateTime excuteTime)
        {
            //加入集合
            lock (excuteBlog)
            {
                TaskInfo info = TaskInfos.FirstOrDefault(item => item.PredictTime.ToString("yyyyMMddHHmmssf") == excuteTime.ToString("yyyyMMddHHmmssf"));
                bool isNewTask = false;
                if (info == null)
                {
                    //添加定时任务
                    TaskInfo preTaskInfo = TaskInfos.LastOrDefault(item => item.PredictTime < excuteTime);
                    int index = TaskInfos.IndexOf(preTaskInfo);
                    info = new TaskInfo();
                    info.PredictTime = excuteTime;
                    info.AddFunc(func);
                    TaskInfos.Insert(index >= 0 ? index + 1 : 0, info);
                    isNewTask = true;

                }
                else
                    //添加处理方法集合
                    info.AddFunc(func);

                if (TimerTask != null)
                {
                    if (isNewTask && TaskInfos.IndexOf(info) == 0)
                    {
                        //如果新插入任务到最开始，需要强制停止当前的时间计时，重新开始计时
                        Monitor.Enter(timerMonitor);
                        Monitor.Pulse(timerMonitor);
                        Monitor.Exit(timerMonitor);
                    }
                }
                else
                {
                    //开始计时
                    TimerTask = StartTimerListen();
                }
            }
        }
        private Task StartTimerListen()
        {
            Task ret = null;
            if (TaskInfos.Count > 0)
            {
                ret = Task.Factory.StartNew(() =>
                {
                    if (TaskInfos.Count == 0) return;
                    TaskInfo curTask = TaskInfos.FirstOrDefault();
                    if (curTask != null)
                    {
                        Monitor.Enter(timerMonitor);
                        if (DateTime.Now >= curTask.PredictTime || !Monitor.Wait(timerMonitor, curTask.PredictTime - DateTime.Now))
                        {
                            //如果是时间到了，则直接执行
                            curTask.RaiseOnTimeOut();
                            //执行后删除
                            TaskInfos.Remove(curTask);
                        }
                        Monitor.Exit(timerMonitor);
                    }
                    TimerTask = StartTimerListen();
                });
            }
            return ret;
        }
    }

    class TaskInfo : IDisposable
    {
        private event EventHandler<EventArgs> OnTimeOut;
        /// <summary>
        /// 预估执行时间
        /// </summary>
        public DateTime PredictTime { set; get; }
        //public List<Action> Items { set; get; }
        List<EventHandler<EventArgs>> Items { set; get; } = new List<EventHandler<EventArgs>>();

        public void AddFunc(Action func)
        {
            EventHandler<EventArgs> taskFunc = (sender, args) => { func(); };
            Items.Add(taskFunc);
            OnTimeOut += taskFunc;
        }

        public void RaiseOnTimeOut()
        {
            Task.Factory.StartNew(() =>
            {
                OnTimeOut?.Invoke(this, null);
                Dispose();
            });
        }

        public void Dispose()
        {
            foreach (EventHandler<EventArgs> taskFunc in Items)
            {
                OnTimeOut -= taskFunc;
            }
            Items.Clear();

        }
    }
}
