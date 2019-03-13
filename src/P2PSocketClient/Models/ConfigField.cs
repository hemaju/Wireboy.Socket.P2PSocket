using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wireboy.Socket.P2PClient.Models
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
