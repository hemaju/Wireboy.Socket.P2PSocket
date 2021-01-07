using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using System;

namespace P2PClientPipe_Plug
{
    public class PlugModule : IP2PSocketPlug
    {
        public string GetPlugName()
        {
            return "客户端命名管道插件";
        }

        public void Init()
        {
            //命名管道，用于与第三方进程通讯
            EasyInject.Put<IPipeServer, ClientPipe>().Singleton();
            EasyInject.Get<IPipeServer>().Start("P2PSocket.Client");
        }
    }
}
