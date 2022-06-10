using P2PSocektLib.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PSocektLib.Export
{
    public interface IP2PServer : IDisposable
    {
        /// <summary>
        /// 开始监听端口
        /// </summary>
        void StartListen();
        /// <summary>
        /// 更新端口映射（如果没有会自动添加）
        /// </summary>
        /// <param name="localPort">本地端口</param>
        /// <param name="remotePort">远端端口</param>
        /// <param name="mode">连接模式</param>
        /// <param name="isSingle">是否单通道模式</param>
        /// <param name="type">网络模式（udp/tcp）</param>
        void UpdatePortMapItem(PortMapItem item);
        /// <summary>
        /// 移除端口映射
        /// </summary>
        /// <param name="localPort">本地端口</param>
        void RemovePortMapItem(int localPort);
    }
}
