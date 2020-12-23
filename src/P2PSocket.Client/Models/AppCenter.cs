using P2PSocket.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            SoftVerSion = "3.3.1";
            RuntimePath = FindRootDir();
            ConfigFile = Path.Combine(RuntimePath, "Client.ini");
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

        protected virtual string FindRootDir()
        {
            DirectoryInfo rootDir = DoFindRootDir(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory));
            if(rootDir == null)
            {
                throw new DllNotFoundException($"在目录{AppDomain.CurrentDomain.BaseDirectory}中，未能找到p2psocket.client.dll");
            }
            else
            {
                return rootDir.FullName;
            }
        }

        protected virtual DirectoryInfo DoFindRootDir(DirectoryInfo pDir)
        {
            if(pDir.GetFiles().FirstOrDefault(file=>file.Name.ToLower() == "p2psocket.client.dll") != null)
            {
                return pDir;
            }
            else
            {
                foreach(DirectoryInfo dir in pDir.GetDirectories())
                {
                    DirectoryInfo result = DoFindRootDir(dir);
                    if (result != null)
                        return result;
                    
                }
                return null;
            }
        }
        /// <summary>
        ///     软件版本
        /// </summary>
        public string SoftVerSion { get; private set; }
        /// <summary>
        ///     运行目录
        /// </summary>
        public string RuntimePath { get; private set; }
        /// <summary>
        ///     配置文件路径
        /// </summary>
        public string ConfigFile { get; private set; }
        /// <summary>
        ///     所有命令集合（需要启动时初始化）
        /// </summary>
        public Dictionary<P2PCommandType, Type> CommandDict { set; get; }
        /// <summary>
        ///     当前主服务Guid
        /// </summary>
        public Guid CurrentGuid { set; get; }
        /// <summary>
        ///     允许处理不经过身份验证的消息类型
        /// </summary>
        public List<P2PCommandType> AllowAnonymous { get; private set; }

        /// <summary>
        /// 上次更新配置的时间
        /// </summary>
        public DateTime LastUpdateConfig { get; set; }

        public AppConfig Config { set; get; }
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
