using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PSocektLib.Utils
{
    internal class Utils_T_AsyncTask<T1,T> where T1 : class 
    {
        Dictionary<T1, TaskCompletionSource<T>> TaskDict = new Dictionary<T1, TaskCompletionSource<T>>();
        /// <summary>
        /// 等待请求-默认5s超时
        /// </summary>
        /// <param name="token">请求的token</param>
        /// <param name="action">等待前执行的方法</param>
        /// <returns></returns>
        public async Task<T> Wait(T1 token, Action? action = null)
        {
            return await Wait(token, action, TimeSpan.FromSeconds(5));
        }
        /// <summary>
        /// 等待请求
        /// </summary>
        /// <param name="token">请求的token</param>
        /// <param name="action">等待前执行的方法</param>
        /// <param name="timeOut">超时时间</param>
        /// <returns></returns>
        public async Task<T> Wait(T1 token, Action? action, TimeSpan timeOut)
        {
            TaskCompletionSource<T> taskCompletionSource = new TaskCompletionSource<T>();
            TaskDict.Add(token, taskCompletionSource);
            action?.Invoke();
            try
            {
                T ret = await taskCompletionSource.Task.WaitAsync(timeOut);
                return ret;
            }
            catch
            {
                // 如果超时，则移除字典中的任务
                if (TaskDict.ContainsKey(token))
                {
                    TaskDict.Remove(token);
                }
                throw;
            }
        }

        /// <summary>
        /// 请求完成，触发回调
        /// </summary>
        /// <param name="token">请求的token</param>
        /// <param name="data">返回的数据</param>
        public void Finish(T1 token, T data)
        {
            if (TaskDict.ContainsKey(token))
            {
                TaskDict[token].SetResult(data);
                TaskDict.Remove(token);
            }
        }
    }
}
