using P2PSocket.Client.Utils;
using P2PSocket.Core.Utils;
using System;
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
            //读取配置文件
            if (ConfigUtils.IsExistConfig())
            {
                //初始化全局变量
                InitGlobal();
                //加载配置文件
                ConfigUtils.LoadFromFile();
                //启动服务
                P2PClient.StartServer();
                //todo:控制台显示
            }
            else
            {
                ConsoleUtils.WriteLine($"启动失败，配置文件不存在.{AppDomain.CurrentDomain.BaseDirectory}/{ Global.ConfigFile}");
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
                .Where(t => typeof(Core.Commands.P2PCommand).IsAssignableFrom(t) && !t.IsAbstract)
                .ToArray();
        }
    }
}
