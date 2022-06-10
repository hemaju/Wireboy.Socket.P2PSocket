using P2PSocektLib.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PSocektLib.Export
{
    public interface IP2PClient
    {
        /// <summary>
        /// 登录服务器-匿名
        /// </summary>
        Task ConnectServer();
        /// <summary>
        /// 登录服务器-使用token登录
        /// </summary>
        /// <param name="token"></param>
        void ConnectServer(string token);
        /// <summary>
        /// 登录服务器-使用账号密码登录
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="psw"></param>
        void ConnectServer(string userName, string psw);
        /// <summary>
        /// 更新端口映射（如果没有会自动添加）
        /// </summary>
        /// <param name="item">映射配置</param>
        void UpdatePortMapItem(PortMapItem item);
        /// <summary>
        /// 移除端口映射
        /// </summary>
        /// <param name="localPort">本地端口</param>
        void RemovePortMapItem(int localPort);
        /// <summary>
        /// 手动创建P2P管道
        /// </summary>
        /// <param name="localPort"></param>
        /// <returns></returns>
        P2PConnect CreateP2PPipe(int localPort);
    }
}
