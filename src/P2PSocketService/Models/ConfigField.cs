using System;
using System.Collections.Generic;
using System.Text;

namespace Wireboy.Socket.P2PService.Models
{
    public class ConfigField : Attribute
    {
        public ConfigField(string remark)
        {
            Remark = remark;
        }
        public String Remark { set; get; }
    }
}
