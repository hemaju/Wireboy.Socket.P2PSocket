using P2PSocket.Client.Utils;
using P2PSocket.Core.Commands;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace P2PSocket.Client
{
    public class CoreModule
    {
        public P2PClient P2PClient = new P2PClient();
        public CoreModule()
        {
            //初始化全局变量
            InitGlobal();
        }
        /// <summary>
        ///     初始化全局变量
        /// </summary>
        protected void InitGlobal()
        {
            InitCommandList();
        }

        /// <summary>
        ///     初始化命令
        /// </summary>
        protected void InitCommandList()
        {
            Type[] commandList = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => typeof(P2PCommand).IsAssignableFrom(t) && !t.IsAbstract)
                .ToArray();
            foreach (Type type in commandList)
            {
                IEnumerable<Attribute> attributes = type.GetCustomAttributes();
                if (!attributes.Any(t => t is CommandFlag))
                {
                    continue;
                }
                CommandFlag flag = attributes.First(t => t is CommandFlag) as CommandFlag;
                if (!AppCenter.Instance.CommandDict.ContainsKey(flag.CommandType))
                {
                    AppCenter.Instance.CommandDict.Add(flag.CommandType, type);
                }
            }
        }

        public void Start()
        {
            LogUtils.InitConfig();
            LogUtils.Info($"客户端版本:{AppCenter.SoftVerSion} 作者：wireboy", false);
            LogUtils.Info($"github地址：https://github.com/bobowire/Wireboy.Socket.P2PSocket", false);
            //读取配置文件
            if (ConfigUtils.IsExistConfig())
            {
                //加载配置文件
                try
                {
                    ConfigCenter config = ConfigUtils.LoadFromFile();
                    ConfigCenter.LoadConfig(config);
                    FileSystemWatcher fw = new FileSystemWatcher(Path.Combine(AppCenter.Instance.RuntimePath, "P2PSocket"), "Client.ini")
                    {
                        NotifyFilter = NotifyFilters.LastWrite
                    };
                    fw.Changed += Fw_Changed;
                    fw.EnableRaisingEvents = true;
                }
                catch (Exception ex)
                {
                    LogUtils.Error($"加载配置文件Client.ini失败：{Environment.NewLine}{ex}");
                    return;
                }
            }
            else
            {
                LogUtils.Error($"找不到配置文件.{AppCenter.Instance.ConfigFile}");
                return;
            }
            //启动服务
            AppCenter.Instance.CurrentGuid = Guid.NewGuid();
            //连接服务器
            P2PClient.ConnectServer();
            AppCenter.Instance.StartNewTask(() => P2PClient.TestAndReconnectServer());
            //启动端口映射
            P2PClient.StartPortMap();
            System.Threading.Thread.Sleep(1000);
        }

        public void Restart(ConfigCenter config)
        {
            CloseTcp();
            System.Threading.Thread.Sleep(2000);
            if (config == null)
            {
                //读取配置文件
                if (ConfigUtils.IsExistConfig())
                {
                    //加载配置文件
                    try
                    {
                        config = ConfigUtils.LoadFromFile();
                    }
                    catch (Exception ex)
                    {
                        LogUtils.Error($"加载配置文件Client.ini失败：{Environment.NewLine}{ex}");
                        return;
                    }
                }
                else
                {
                    LogUtils.Error($"找不到配置文件.{AppCenter.Instance.ConfigFile}");
                    return;
                }
            }
            ConfigCenter.LoadConfig(config);
            //启动服务
            AppCenter.Instance.CurrentGuid = Guid.NewGuid();
            //连接服务器
            P2PClient.ConnectServer();
            AppCenter.Instance.StartNewTask(() => P2PClient.TestAndReconnectServer());
            //启动端口映射
            P2PClient.StartPortMap();
        }

        DateTime lastUpdateConfig = DateTime.Now;
        object fwObj = new object();
        private void Fw_Changed(object sender, FileSystemEventArgs e)
        {
            DateTime curTime = DateTime.Now;
            lock (fwObj)
            {
                if (DateTime.Compare(lastUpdateConfig.AddSeconds(5), curTime) > 0) return;
                lastUpdateConfig = DateTime.Now;
                Restart(null);
            }
        }

        public void CloseTcp()
        {
            AppCenter.Instance.CurrentGuid = Guid.NewGuid();
            TcpCenter.Instance.P2PServerTcp?.SafeClose();
            foreach (var listener in TcpCenter.Instance.ListenerList.Values)
            {
                listener.Stop();
            }
            TcpCenter.Instance.ListenerList.Clear();
        }

    }
}
