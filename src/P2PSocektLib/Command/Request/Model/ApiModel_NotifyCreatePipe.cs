using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PSocektLib.Command
{
    /// <summary>
    /// 通知建立管道
    /// </summary>
    internal class ApiModel_NotifyCreatePipe
    {
        public string PipeToken { set; get; }
        public ApiModel_NotifyCreatePipe(string token)
        {
            PipeToken = token;
        }
    }
}
