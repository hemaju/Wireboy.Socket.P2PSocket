using P2PSocket.Client.Utils;
using P2PSocket.Core.Commands;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace P2PSocket.Client
{
    public class CoreModule
    {
        private P2PClient P2PClient = new P2PClient();
        public CoreModule()
        {
        }

        public void Start()
        {
            LogUtils.InitConfig();
            LogUtils.Show($"程序版本:{Global.SoftVerSion}  通讯协议:{Global.DataVerSion}");
            //读取配置文件
            if (ConfigUtils.IsExistConfig())
            {
                //初始化全局变量
                InitGlobal();
                //加载配置文件
                try
                {
                    ConfigUtils.LoadFromFile();
                }
                catch (Exception ex)
                {
                    LogUtils.Error($"配置文件格式错误.");
                    return;
                }
                //启动服务
                P2PClient.StartServer();
            }
            else
            {
                LogUtils.Error($"找不到配置文件：\"{ Global.ConfigFile}\"");
            }
        }

        public void Stop()
        {
            Global.CurrentGuid = Guid.NewGuid();
            foreach (var listener in P2PClient.ListenerList)
            {
                listener.Stop();
            }
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
                if (!Global.CommandDict.ContainsKey(flag.CommandType))
                {
                    Global.CommandDict.Add(flag.CommandType, type);
                }
            }
        }
    }
}
