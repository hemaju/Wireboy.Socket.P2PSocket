using P2PSocket.Client;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TransferEncryption_Plug
{
    public class PlugModule : IP2PSocketPlug
    {
        AppCenter appCenter = EasyInject.Get<AppCenter>();
        public string GetPlugName()
        {
            return "0x0202命令数据加密插件";
        }

        public void Init()
        {
            AddCommand<Cmd_0x0202Ex>();
        }

        public void AddCommand<CmdClass>()
        {

            IEnumerable<Attribute> attributes = typeof(CmdClass).GetCustomAttributes();
            if (attributes.Any(t => t is CommandFlag))
            {
                CommandFlag flag = attributes.First(t => t is CommandFlag) as CommandFlag;
                if (!appCenter.CommandDict.ContainsKey(flag.CommandType))
                {
                    appCenter.CommandDict.Add(flag.CommandType, typeof(CmdClass));
                }
                else
                {
                    appCenter.CommandDict[flag.CommandType] = typeof(CmdClass);
                }
            }
        }
    }
}
