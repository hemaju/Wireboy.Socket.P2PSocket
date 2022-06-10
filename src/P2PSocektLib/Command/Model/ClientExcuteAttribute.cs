using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PSocektLib.Command
{
    internal class ClientExcuteAttribute : Attribute
    {
        public RequestEnum RequestType { set; get; }
        public ClientExcuteAttribute(RequestEnum type)
        {
            RequestType = type;
        }
    }
}
