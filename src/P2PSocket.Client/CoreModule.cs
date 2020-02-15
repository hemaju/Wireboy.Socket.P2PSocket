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
                if (!Global.CommandDict.ContainsKey(flag.CommandType))
                {
                    Global.CommandDict.Add(flag.CommandType, type);
                }
            }
        }

        public void Start()
        {
            LogUtils.InitConfig();
            LogUtils.Info($"程序版本:{Global.SoftVerSion}  通讯协议:{Global.DataVerSion}", false);
            //读取配置文件
            if (ConfigUtils.IsExistConfig())
            {
                //加载配置文件
                try
                {
                    ConfigUtils.LoadFromFile();
                    FileSystemWatcher fw = new FileSystemWatcher(Path.Combine(Global.RuntimePath, "P2PSocket"),"*.ini");
                    fw.NotifyFilter = NotifyFilters.LastWrite;
                    fw.Changed += Fw_Changed;
                    fw.EnableRaisingEvents = true;

                }
                catch (Exception ex)
                {
                    LogUtils.Error($"加载配置文件Client.ini失败：{Environment.NewLine}{ex}");
                    return;
                }
                //启动服务
                Global.CurrentGuid = Guid.NewGuid();
                //连接服务器
                P2PClient.ConnectServer();
                Global.TaskFactory.StartNew(() => P2PClient.TestAndReconnectServer());
                //启动端口映射
                P2PClient.StartPortMap();
            }
            else
            {
                LogUtils.Error($"找不到配置文件.{Global.ConfigFile}");
            }
            System.Threading.Thread.Sleep(1000);
        }

        private void Fw_Changed(object sender, FileSystemEventArgs e)
        {
            Stop();
            System.Threading.Thread.Sleep(2000);
            Start();
        }

        public void ReloadConfig(bool isServerAddressChanged = false)
        {
            if (isServerAddressChanged)
            {
                LogUtils.Trace($"服务器地址变化，{Global.P2PServerTcp.RemoteEndPoint.ToString()} -> {Global.ServerAddress}:{Global.ServerPort}");
                Global.P2PServerTcp.Close();
            }
            LogUtils.Trace("开始更新本地监听端口");
            P2PClient.StartPortMap();
        }

        public void Stop()
        {
            Global.CurrentGuid = Guid.NewGuid();
            Global.P2PServerTcp?.Close();
            foreach (var listener in P2PClient.ListenerList.Values)
            {
                listener.Stop();
            }
            Global.Init();
        }

    }
}
