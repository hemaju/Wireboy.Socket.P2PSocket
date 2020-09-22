using P2PSocket.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace P2PSocket.Server
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
            ConfigFile = Path.Combine(RuntimePath, "P2PSocket", "Server.ini");
            MacMapFile = Path.Combine(RuntimePath, "P2PSocket", "Server_mac.cfc");
            CurrentGuid = Guid.NewGuid();
            CommandDict = new Dictionary<P2PCommandType, Type>();
            AllowAnonymous = new List<P2PCommandType>();
            AllowAnonymous.Add(P2PCommandType.Heart0x0052);
            AllowAnonymous.Add(P2PCommandType.Login0x0101);
            AllowAnonymous.Add(P2PCommandType.Login0x0103);
            AllowAnonymous.Add(P2PCommandType.Login0x0104);
            AllowAnonymous.Add(P2PCommandType.P2P0x0211);
            AllowAnonymous.Add(P2PCommandType.P2P0x0201);
            AllowAnonymous.Add(P2PCommandType.Msg0x0301);
            Config = new AppConfig();
        }
        /// <summary>
        ///     软件版本
        /// </summary>
        public string SoftVerSion { get; private set; }
        /// <summary>
        ///     通讯协议版本
        /// </summary>
        /// <summary>
        ///     运行目录
        /// </summary>
        public string RuntimePath { get; private set; }
        /// <summary>
        ///     配置文件路径
        /// </summary>
        public string ConfigFile { get; private set; }
        /// <summary>
        ///     配置文件路径
        /// </summary>
        public string MacMapFile { get; private set; }
        /// <summary>
        ///     当前主服务Guid
        /// </summary>
        public Guid CurrentGuid { set; get; }
        /// <summary>
        ///     所有命令集合（需要启动时初始化）
        /// </summary>
        public Dictionary<P2PCommandType, Type> CommandDict { set; get; }
        /// <summary>
        ///     无需授权的接口集合
        /// </summary>
        public List<P2PCommandType> AllowAnonymous { get; private set; }

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
