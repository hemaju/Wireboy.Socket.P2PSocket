using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;
using P2PSocket.Core.Commands;
using P2PSocket.Server.Utils;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;

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
            LogUtils.InitConfig();
            LogUtils.Show($"程序版本:{Global.SoftVerSion}  通讯协议:{Global.DataVerSion}");
            //读取配置文件
            if (ConfigUtils.IsExistConfig())
            {
                //初始化全局变量
                InitGlobal();
                //加载配置文件
                ConfigUtils.LoadFromFile();
                //启动服务
                P2PServer.StartServer();
                //todo:控制台显示
            }
            else
            {
                LogUtils.Show("启动失败，配置文件不存在.");
            }
        }

        public void Stop()
        {
            Global.CurrentGuid = Guid.NewGuid();
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
                if (!Global.CommandDict.ContainsKey(flag.CommandType))
                {
                    Global.CommandDict.Add(flag.CommandType, type);
                }
            }
        }
    }
}
