using P2PSocket.Client.Models;
using P2PSocket.Client.Utils;
using P2PSocket.Core.Commands;
using P2PSocket.Core.CoreImpl;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace P2PSocket.Client
{
    public class CoreModule
    {
        public P2PClient P2PClient;
        AppCenter appCenter { set; get; }
        TcpCenter tcpCenter { set; get; }
        IConfig configManager { set; get; }
        IPipeServer pipeServer { set; get; }
        public CoreModule()
        {
            int minWorker, minIOC;
            // Get the current settings.
            ThreadPool.GetMinThreads(out minWorker, out minIOC);
            ThreadPool.SetMinThreads(20, minIOC);
            //初始化全局变量
            InitGlobal();
        }
        /// <summary>
        ///     初始化全局变量
        /// </summary>
        protected virtual void InitGlobal()
        {
            InitRegister();
            InitInterface();
            InitCommandList();
            LoadConfig();
            pipeServer.Start();
            LoadPlugs();
        }

        /// <summary>
        ///     初始化命令
        /// </summary>
        protected virtual void InitCommandList()
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
                if (!appCenter.CommandDict.ContainsKey(flag.CommandType))
                {
                    appCenter.CommandDict.Add(flag.CommandType, type);
                }
            }
        }
        protected virtual void InitRegister()
        {
            EasyInject.Put<AppCenter, AppCenter>().Singleton();
            EasyInject.Put<TcpCenter, TcpCenter>().Singleton();
            EasyInject.Put<IFileManager, FileManager>().Common();
            EasyInject.Put<ILogger, Logger>().Singleton();
            EasyInject.Put<IConfig, ConfigManager>().Singleton();
            EasyInject.Put<IPipeServer, PipeServer>().Singleton();
            EasyInject.Put<P2PClient, P2PClient>().Singleton();
        }
        protected virtual void InitInterface()
        {
            appCenter = EasyInject.Get<AppCenter>();
            tcpCenter = EasyInject.Get<TcpCenter>();
            configManager = EasyInject.Get<IConfig>();
            P2PClient = EasyInject.Get<P2PClient>();
            pipeServer = EasyInject.Get<IPipeServer>();
        }

        /// <summary>
        /// 加载插件
        /// </summary>
        protected virtual void LoadPlugs()
        {
            DirectoryInfo plugDir = new DirectoryInfo(Path.Combine(appCenter.RuntimePath, "Plugs"));
            if (plugDir != null)
            {
                foreach (string file in plugDir.GetFiles().Select(t => t.FullName).Where(t => t.ToLower().EndsWith(".dll")))
                {
                    EasyOp.Do(() =>
                    {
                    //载入dll
                    bool flag = false;
                        Assembly ab = Assembly.LoadFrom(file);
                        Type[] types = ab.GetTypes();
                        foreach (Type curInstance in types)
                        {
                            if (curInstance.GetInterface("IP2PSocketPlug") != null)
                            {
                                IP2PSocketPlug instance = Activator.CreateInstance(curInstance) as IP2PSocketPlug;
                                LogUtils.Info($"成功加载插件 {instance.GetPlugName()}");
                                instance.Init();
                                break;
                            }
                        }
                    },
                    ex =>
                    {
                        LogUtils.Warning($"加载插件失败 >> {ex}");
                    });
                }
            }
        }

        public virtual void Start()
        {
            //启动服务
            appCenter.CurrentGuid = Guid.NewGuid();
            //连接服务器
            P2PClient.ConnectServer();
            appCenter.StartNewTask(() => P2PClient.TestAndReconnectServer());
            //启动端口映射
            P2PClient.StartPortMap();
            Thread.Sleep(1000);
        }

        protected virtual void LoadConfig()
        {
            LogUtils.Info($"客户端版本:{appCenter.SoftVerSion} 作者：wireboy", false);
            LogUtils.Info($"github地址：https://github.com/bobowire/Wireboy.Socket.P2PSocket", false);
            //读取配置文件
            if (configManager.IsExistConfig())
            {
                //加载配置文件
                try
                {
                    appCenter.Config = configManager.LoadFromFile() as AppConfig;
                    FileSystemWatcher fw = new FileSystemWatcher(appCenter.RuntimePath, "Client.ini")
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
                LogUtils.Error($"找不到配置文件.{appCenter.ConfigFile}");
                return;
            }
        }

        public virtual void Restart(AppConfig config)
        {
            //关闭所有端口监听
            CloseTcp();
            Thread.Sleep(2000);
            if (config == null)
            {
                //读取配置文件
                if (configManager.IsExistConfig())
                {
                    //加载配置文件
                    try
                    {
                        appCenter.Config = configManager.LoadFromFile() as AppConfig;
                    }
                    catch (Exception ex)
                    {
                        LogUtils.Error($"加载配置文件Client.ini失败：{Environment.NewLine}{ex}");
                        return;
                    }
                }
                else
                {
                    LogUtils.Error($"找不到配置文件.{appCenter.ConfigFile}");
                    return;
                }
            }
            //启动服务
            appCenter.CurrentGuid = Guid.NewGuid();
            //连接服务器
            P2PClient.ConnectServer();
            appCenter.StartNewTask(() => P2PClient.TestAndReconnectServer());
            //启动端口映射
            P2PClient.StartPortMap();
        }

        object fwObj = new object();
        protected virtual void Fw_Changed(object sender, FileSystemEventArgs e)
        {
            DateTime curTime = DateTime.Now;
            lock (fwObj)
            {
                if (DateTime.Compare(appCenter.LastUpdateConfig.AddSeconds(5), curTime) > 0) return;
                appCenter.LastUpdateConfig = DateTime.Now;
                Restart(null);
            }
        }

        public virtual void CloseTcp()
        {
            appCenter.CurrentGuid = Guid.NewGuid();
            tcpCenter.P2PServerTcp?.SafeClose();
            foreach (var listener in tcpCenter.ListenerList.Values)
            {
                listener.Stop();
            }
            tcpCenter.ListenerList.Clear();
        }

    }
}
