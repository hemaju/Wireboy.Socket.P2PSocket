using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PSocektLib.Command
{
    /// <summary>
    /// 使用管道新建一个连接
    /// </summary>
    internal class ApiModel_Pipe_NotifyCreateConn
    {
        /// <summary>
        /// 远端端口
        /// </summary>
        public int RemotePort { set; get; }
        public ApiModel_Pipe_NotifyCreateConn(int remotePort)
        {
            RemotePort = remotePort;
        }
    }


    /// <summary>
    /// 使用管道新建一个连接
    /// </summary>
    internal class ApiModel_Pipe_NotifyCreateConn_R
    {
        public bool IsSuccess { set;get; }
        public string? Message { set; get; }
        public ApiModel_Pipe_NotifyCreateConn_R(bool isSuccess)
        {
            IsSuccess = isSuccess;
        }
    }
}
