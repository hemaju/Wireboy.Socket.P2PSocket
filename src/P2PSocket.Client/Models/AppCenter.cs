using P2PSocket.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace P2PSocket.Client
{
    public class AppCenter
    {
        public AppCenter()
        {
            Init();
        }

        private void Init()
        {
            SoftVerSion = "3.1.0";
            RuntimePath = AppDomain.CurrentDomain.BaseDirectory;
            ConfigFile = Path.Combine(RuntimePath, "P2PSocket", "Client.ini");
            CommandDict = new Dictionary<P2PCommandType, Type>();
            CurrentGuid = new Guid();
            Config = new AppConfig();
            AllowAnonymous = new List<P2PCommandType>();
            AllowAnonymous.Add(P2PCommandType.Heart0x0052);
            AllowAnonymous.Add(P2PCommandType.Login0x0101);
            AllowAnonymous.Add(P2PCommandType.P2P0x0211);
            AllowAnonymous.Add(P2PCommandType.P2P0x0201);
            LastUpdateConfig = DateTime.Now;
        }
        /// <summary>
        ///     软件版本
        /// </summary>
        public string SoftVerSion { get; private set; }
        /// <summary>
        ///     运行目录
        /// </summary>
        internal string RuntimePath { get; private set; }
        /// <summary>
        ///     配置文件路径
        /// </summary>
        internal string ConfigFile { get; private set; }
        /// <summary>
        ///     所有命令集合（需要启动时初始化）
        /// </summary>
        internal Dictionary<P2PCommandType, Type> CommandDict { set; get; }
        /// <summary>
        ///     当前主服务Guid
        /// </summary>
        internal Guid CurrentGuid { set; get; }
        /// <summary>
        ///     允许处理不经过身份验证的消息类型
        /// </summary>
        internal List<P2PCommandType> AllowAnonymous { get; private set; }

        /// <summary>
        /// 上次更新配置的时间
        /// </summary>
        internal DateTime LastUpdateConfig { get; set; }

        internal AppConfig Config { set; get; }
        /// <summary>
        /// 在新线程执行任务
        /// </summary>
        /// <param name="action">任务</param>
        public void StartNewTask(Action action)
        {
            ThreadPool.QueueUserWorkItem(obj =>
            {
                action();
            });
        }
    }
}
