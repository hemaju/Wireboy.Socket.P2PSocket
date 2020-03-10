using P2PSocket.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace P2PSocket.Server
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
        public string SoftVerSion { get; } = "3.0.2";
        /// <summary>
        ///     通讯协议版本
        /// </summary>
        /// <summary>
        ///     运行目录
        /// </summary>
        public string RuntimePath { get { return AppDomain.CurrentDomain.BaseDirectory; } }
        /// <summary>
        ///     配置文件路径
        /// </summary>
        public string ConfigFile { get { return Path.Combine(RuntimePath, "P2PSocket", "Server.ini"); } }
        /// <summary>
        ///     当前主服务Guid
        /// </summary>
        public Guid CurrentGuid { set; get; } = Guid.NewGuid();
        /// <summary>
        ///     所有命令集合（需要启动时初始化）
        /// </summary>
        public Dictionary<P2PCommandType, Type> CommandDict { set; get; } = new Dictionary<P2PCommandType, Type>();
        /// <summary>
        ///     无需授权的接口集合
        /// </summary>
        public List<P2PCommandType> AllowAnonymous { get; } = new List<P2PCommandType>() { P2PCommandType.Heart0x0052
            , P2PCommandType.Login0x0101
            , P2PCommandType.Login0x0103
            , P2PCommandType.P2P0x0211
            , P2PCommandType.P2P0x0201
            , P2PCommandType.Msg0x0301 };
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
