using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;
using P2PSocket.Core.Commands;
using P2PSocket.Server.Utils;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using System.IO;
using P2PSocket.Core.CoreImpl;
using P2PSocket.Server.Models;

namespace P2PSocket.Server
{
    public class CoreModule
    {
        private P2PServer P2PServer = new P2PServer();
        ClientCenter clientCenter = EasyInject.Get<ClientCenter>();
        AppCenter appCenter { set; get; }
        IServerConfig configManager { set; get; }
        public CoreModule()
        {
            InitGlobal();
        }

        public void Start()
        {
            try
            {
                LogUtils.Info($"客户端版本:{appCenter.SoftVerSion} 作者：wireboy", false);
                LogUtils.Info($"github地址：https://github.com/bobowire/Wireboy.Socket.P2PSocket", false);
                //读取配置文件
                if (configManager.IsExistConfig())
                {
                    //加载配置文件
                    appCenter.Config = configManager.LoadFromFile() as AppConfig; ;
                    FileSystemWatcher fw = new FileSystemWatcher(Path.Combine(appCenter.RuntimePath, "P2PSocket"), "Server.ini")
                    {
                        NotifyFilter = NotifyFilters.LastWrite
                    };
                    fw.Changed += Fw_Changed;
                    fw.EnableRaisingEvents = true;
                    //启动服务
                    P2PServer.StartServer();
                }
                else
                {
                    LogUtils.Error($"找不到配置文件.{appCenter.ConfigFile}");
                }
            }
            catch(Exception ex)
            {
                LogUtils.Error($"启动失败：{ex}");
            }
            System.Threading.Thread.Sleep(1000);
        }

        public void Stop()
        {
            appCenter.CurrentGuid = Guid.NewGuid();
            foreach (var listener in P2PServer.ListenerList)
            {
                listener.Stop();
            }
            P2PServer.ListenerList.Clear();
            foreach (var tcpItem in clientCenter.TcpMap)
            {
                tcpItem.Value.TcpClient.Close();
            }
            clientCenter.TcpMap.Clear();
        }
        /// <summary>
        ///     初始化全局变量
        /// </summary>
        public void InitGlobal()
        {
            InitRegister();
            InitSelf();
            InitCommandList();
        }
        protected void InitRegister()
        {
            EasyInject.Put<AppCenter, AppCenter>().Singleton();
            EasyInject.Put<IFileManager, FileManager>().Common();
            EasyInject.Put<ILogger, Logger>().Singleton();
            EasyInject.Put<IServerConfig, ConfigManager>().Singleton();
            EasyInject.Put<ClientCenter, ClientCenter>().Singleton();
        }
        protected void InitSelf()
        {
            appCenter = EasyInject.Get<AppCenter>();
            configManager = EasyInject.Get<IServerConfig>();
            P2PServer = new P2PServer();
        }

        /// <summary>
        ///     初始化命令
        /// </summary>
        public void InitCommandList()
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

        public void Restart(AppConfig config)
        {
            Stop();
            System.Threading.Thread.Sleep(2000);
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
                        LogUtils.Error($"加载配置文件Server.ini失败：{Environment.NewLine}{ex}");
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
            //启动服务
            P2PServer.StartServer();
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
    }
}
