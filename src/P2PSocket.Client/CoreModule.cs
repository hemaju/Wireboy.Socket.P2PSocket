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
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            pipeServer.Start("P2PSocket.Client");
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
            //应用中心，用于存放配置、全局变量等信息
            EasyInject.Put<AppCenter, AppCenter>().Singleton();
            //Tcp管理，用于保存当前tcp连接和端口监听实例
            EasyInject.Put<TcpCenter, TcpCenter>().Singleton();
            //文件IO接口，不同系统的文件读写方式有区别，可重载此接口实现定制
            EasyInject.Put<IFileManager, FileManager>().Common();
            //日志接口，用于日志写入
            EasyInject.Put<ILogger, Logger>().Singleton();
            //配置管理，用于读写配置文件
            EasyInject.Put<IConfig, ConfigManager>().Singleton();
            //命名管道，用于与第三方进程通讯
            EasyInject.Put<IPipeServer, PipeServer>().Singleton();
            //内网穿透客户端实例
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
            string plugPath = Path.Combine(appCenter.RuntimePath, "Plugs");
            if (Directory.Exists(plugPath))
            {
                DirectoryInfo plugDir = new DirectoryInfo(plugPath);
                if (plugDir != null)
                {
                    foreach (string file in plugDir.GetFiles().Select(t => t.FullName).Where(t => t.ToLower().EndsWith(".dll")))
                    {
                        EasyOp.Do(() =>
                        {
                        //载入dll
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

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogUtils.Fatal($"发生未处理的异常：{e.ExceptionObject}");
        }

        protected virtual void LoadConfig()
        {
            LogUtils.Info($"客户端版本:{appCenter.SoftVerSion} 作者：wireboy", false);
            LogUtils.Info($"github地址：https://github.com/hemaju/Wireboy.Socket.P2PSocket", false);
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
