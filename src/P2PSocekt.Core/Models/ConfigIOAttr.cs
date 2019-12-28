using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Core.Models
{
    public class ConfigIOAttr :Attribute
    {
        public string Name { set; get; }
        public ConfigIOAttr(string name)
        {
            Name = name;
        }
    }
}
