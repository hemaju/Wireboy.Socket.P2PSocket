using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Core.Models
{
    public class ConfigMethodAttr : Attribute
    {
        public string Name { set; get; }
        public ConfigMethodAttr(string name)
        {
            Name = name;
        }
    }
}
