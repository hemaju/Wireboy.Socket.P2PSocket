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

namespace P2PSocket.Server
{
    public class CoreModule
    {
        private P2PServer P2PServer = new P2PServer();
        public CoreModule()
        {

        }

        public void Start()
        {
            try
            {
                LogUtils.InitConfig();
                LogUtils.Info($"客户端版本:{AppCenter.Instance.SoftVerSion} 作者：wireboy", false);
                LogUtils.Info($"github地址：https://github.com/bobowire/Wireboy.Socket.P2PSocket", false);
                //读取配置文件
                if (ConfigUtils.IsExistConfig())
                {
                    //初始化全局变量
                    InitGlobal();
                    //加载配置文件
                    ConfigCenter config = ConfigUtils.LoadFromFile();
                    ConfigCenter.LoadConfig(config);
                    FileSystemWatcher fw = new FileSystemWatcher(Path.Combine(AppCenter.Instance.RuntimePath, "P2PSocket"), "Server.ini")
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
                    LogUtils.Error($"找不到配置文件.{AppCenter.Instance.ConfigFile}");
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
            AppCenter.Instance.CurrentGuid = Guid.NewGuid();
            foreach (var listener in P2PServer.ListenerList)
            {
                listener.Stop();
            }
            P2PServer.ListenerList.Clear();
            foreach (var tcpItem in ClientCenter.Instance.TcpMap)
            {
                tcpItem.Value.TcpClient.Close();
            }
            ClientCenter.Instance.TcpMap.Clear();
        }
        /// <summary>
        ///     初始化全局变量
        /// </summary>
        public void InitGlobal()
        {
            InitCommandList();
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
                if (!AppCenter.Instance.CommandDict.ContainsKey(flag.CommandType))
                {
                    AppCenter.Instance.CommandDict.Add(flag.CommandType, type);
                }
            }
        }

        public void Restart(ConfigCenter config)
        {
            Stop();
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
                        LogUtils.Error($"加载配置文件Server.ini失败：{Environment.NewLine}{ex}");
                        return;
                    }
                }
                else
                {
                    LogUtils.Error($"找不到配置文件.{AppCenter.Instance.ConfigFile}");
                    return;
                }
            }
            //启动服务
            AppCenter.Instance.CurrentGuid = Guid.NewGuid();
            ConfigCenter.LoadConfig(config);
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
