using P2PSocket.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace P2PSocket.Client
{
    public class AppCenter
    {
        static AppCenter m_instance = null;
        public static AppCenter Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new AppCenter();
                }
                return m_instance;
            }
        }
        private AppCenter()
        {

        }
        /// <summary>
        ///     软件版本
        /// </summary>
        public const string SoftVerSion = "3.0.2";
        /// <summary>
        ///     运行目录
        /// </summary>
        internal string RuntimePath { get { return AppDomain.CurrentDomain.BaseDirectory; } }
        /// <summary>
        ///     配置文件路径
        /// </summary>
        internal string ConfigFile { get { return Path.Combine(RuntimePath, "P2PSocket", "Client.ini"); } }
        /// <summary>
        ///     所有命令集合（需要启动时初始化）
        /// </summary>
        internal Dictionary<P2PCommandType, Type> CommandDict { set; get; } = new Dictionary<P2PCommandType, Type>();
        /// <summary>
        ///     当前主服务Guid
        /// </summary>
        internal Guid CurrentGuid { set; get; } = Guid.NewGuid();
        /// <summary>
        ///     允许处理不经过身份验证的消息类型
        /// </summary>
        internal List<P2PCommandType> AllowAnonymous { get; } = new List<P2PCommandType>() { P2PCommandType.Heart0x0052, P2PCommandType.Login0x0101, P2PCommandType.P2P0x0211, P2PCommandType.P2P0x0201 };
        /// <summary>
        /// 在新线程执行任务
        /// </summary>
        /// <param name="action">任务</param>
        public void StartNewTask(Action action)
        {
            Task.Factory.StartNew(action);   
        }
    }
}
