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
                    //启动服务
                    P2PServer.StartServer();
                    //todo:控制台显示
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
    }
}
