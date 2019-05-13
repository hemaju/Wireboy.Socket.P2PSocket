using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;
using P2PSocket.Core.Commands;
using P2PSocket.Server.Utils;

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
                Console.WriteLine("启动失败，配置文件不存在.");
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
            Global.CommandList = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t=> typeof(P2PCommand).IsAssignableFrom(t) && !t.IsAbstract)
                .ToArray();
        }
    }
}
