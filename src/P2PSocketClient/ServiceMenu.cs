using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        }
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

        public void DrawMainMenu()
        {
            Console.WriteLine("-------------主菜单-------------");
            Console.WriteLine("1.仅被控端启动    2.仅主控端启动");
            Console.WriteLine("3.主控被控启动    4.查看服务信息");
        }
        public void DoMainMenu(int key)
        {
            switch (key)
            {
                case 1:
                    {
                        Console.WriteLine("选择了菜单1");
                        _consoleTips = "被控服务启动成功！";
                    }
                    break;
                case 2:
                    {
                        Console.WriteLine("选择了菜单2");
                        _consoleTips = "主控服务启动成功！";
                    }
                    break;
                case 3:
                    {
                        Console.WriteLine("选择了菜单3");
                        _consoleTips = "主控、被控服务启动成功！";
                    }
                    break;
                case 4:
                    {
                        Console.WriteLine("选择了菜单4");
                    }
                    break;
                default:
                    {
                        Console.WriteLine("选择的菜单不存在！");
                    }
                    break;
            }
        }

        public void StartHomeClient()
        {

        }

        public void StartClient()
        {

        }

        public void StartAllClient()
        {

        }

        public void ShowState()
        {

        }
    }
}
