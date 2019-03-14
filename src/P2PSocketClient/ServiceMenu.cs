using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wireboy.Socket.P2PClient;

namespace P2PServiceHome
{
    public class ServiceMenu
    {
        P2PService _p2pService = null;
        private string _consoleError = "";
        private string _consoleTips = "";

        public ServiceMenu()
        {
            _p2pService = new P2PService();
            _p2pService.ConnectServer();
        }
        /// <summary>
        /// 显示菜单
        /// </summary>
        public void ShowMenu()
        {
            while (true)
            {
                if (!string.IsNullOrEmpty(_consoleError))
                {
                    Console.WriteLine("错误：{0}", _consoleError);
                    _consoleError = "";
                }
                if (!string.IsNullOrEmpty(_consoleTips))
                {
                    Console.WriteLine("提示：{0}", _consoleTips);
                    _consoleTips = "";
                }
                DrawMainMenu();
                int key;
                if (ReadKey(out key))
                {
                    DoMainMenu(key);
                }
            }
        }
        /// <summary>
        /// 获取菜单序号
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ReadKey(out int key)
        {
            Console.WriteLine("请输入菜单序号：");
            String key_str = Console.ReadLine();
            bool ret = true;
            if (!int.TryParse(key_str, out key))
            {
                _consoleError = "请输入菜单序号！例如：1";
                ret = false;
            }
            Console.Clear();
            return ret;
        }

        /// <summary>
        /// 绘制主菜单
        /// </summary>
        public void DrawMainMenu()
        {
            Console.WriteLine("-------------主菜单-------------");
            Console.WriteLine("1.仅被控端启动    2.仅主控端启动");
            Console.WriteLine("3.主控被控启动    4.功能测试");
        }

        /// <summary>
        /// 处理主菜单输入
        /// </summary>
        /// <param name="key"></param>
        public void DoMainMenu(int key)
        {
            switch (key)
            {
                case 1:
                    {
                        StartHomeServer();
                    }
                    break;
                case 2:
                    {
                        StartClientServer();
                    }
                    break;
                case 3:
                    {
                        StartAllClient();
                    }
                    break;
                case 4:
                    {
                        TestServer();
                    }
                    break;
                default:
                    {
                        Console.WriteLine("选择的菜单不存在！");
                    }
                    break;
            }
        }

        public void StartHomeServer()
        {
            Console.WriteLine("请输入本地Home服务名称：");
            String homeName = Console.ReadLine();
            _p2pService.StartHomeServer(homeName);
            Console.WriteLine("本地Home服务名称：{0}",homeName);
        }

        public void StartClientServer()
        {
            Console.WriteLine("请输入要连接的Home服务名称：");
            String homeName = Console.ReadLine();
            _p2pService.StartClientServer(homeName);
            Console.WriteLine("要连接的Home服务名称：{0}", homeName);
        }

        public void StartAllClient()
        {
            StartHomeServer();
            StartClientServer();
        }

        public void TestServer()
        {
            ConfigServer.SaveToFile();
            //_p2pService.TestServer();
        }
    }
}
