using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Wireboy.Socket.P2PClient
{
    public class ServiceMenu
    {
        P2PService _p2pService = null;

        public ServiceMenu()
        {
            ConfigServer.LoadFromFile();
            _p2pService = new P2PService();
            //_p2pService.ConnectServer();
        }
        /// <summary>
        /// 显示菜单
        /// </summary>
        public void ShowMenu()
        {
            if (_p2pService.Start())
            {
                while (true)
                {
                    Thread.Sleep(500);
                    Logger.Console.WriteLine("请输入远程服务名称：");
                    string remoteServerName = Console.ReadLine();
                    if (!_p2pService.SetRemoteServerName(remoteServerName))
                    {
                        Logger.Error.WriteLine("远程服务-服务不可用！");
                    }
                    Console.ReadKey();
                }
            }
            else
            {
                Logger.Console.WriteLine("任意键退出...");
                Console.ReadKey();
            }
        }
    }
}
