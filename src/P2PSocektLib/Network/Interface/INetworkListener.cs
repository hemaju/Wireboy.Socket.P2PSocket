using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PSocektLib
{
    /// <summary>
    /// 新连入客户端回调方法
    /// </summary>
    /// <param name="sender">新连入连接</param>
    /// <returns></returns>
    public delegate void AcceptConnectionEventCallback(INetworkConnect sender);
    public interface INetworkListener
    {
        /// <summary>
        /// 绑定端口
        /// </summary>
        /// <param name="port"></param>
        void Bind(int port);
        /// <summary>
        /// 开始监听
        /// </summary>
        void Start();
        /// <summary>
        /// 停止监听
        /// </summary>
        void Stop();
        /// <summary>
        /// 绑定新传入连接事件
        /// </summary>
        /// <param name="action">回调方法</param>
        void BindAcceptConnectionEvent(AcceptConnectionEventCallback action);
    }
}
