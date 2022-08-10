using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PSocektLib.Command
{
    internal class ApiModel_CreatePipe
    {
        public string PipeToken { set; get; }
        public ApiModel_CreatePipe(string token)
        {
            PipeToken = token;
        }
    }
}
