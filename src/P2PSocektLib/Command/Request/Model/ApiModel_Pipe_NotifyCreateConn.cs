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
        /// 本地连接id
        /// </summary>
        public int LocalId { set; get; }
        /// <summary>
        /// 请求的token
        /// </summary>
        public string PipeToken { set; get; }
        public ApiModel_Pipe_NotifyCreateConn(string token, int localId)
        {
            PipeToken = token;
            LocalId = localId;
        }
    }


    /// <summary>
    /// 使用管道新建一个连接
    /// </summary>
    internal class ApiModel_Pipe_NotifyCreateConn_R
    {
        /// <summary>
        /// 远端连接id
        /// </summary>
        public int? RemoteId { set; get; }
        /// <summary>
        /// 本地连接id
        /// </summary>

        public int? LocalId { set; get; }
    }
}
